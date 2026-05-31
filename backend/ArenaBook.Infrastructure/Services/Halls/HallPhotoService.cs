using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Common.Exceptions;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;
using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Persistence;
using ArenaBook.Infrastructure.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Halls;

public sealed class HallPhotoService : IHallPhotoService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateHallPhotoRequest> _createValidator;
    private readonly IValidator<UpdateHallPhotoRequest> _updateValidator;

    public HallPhotoService(
        ArenaBookDbContext db,
        IValidator<CreateHallPhotoRequest> createValidator,
        IValidator<UpdateHallPhotoRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<HallPhotoResponse>> GetPagedAsync(
        int hallId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var hallExists = await _db.Halls.AsNoTracking().AnyAsync(h => h.Id == hallId, cancellationToken);
        if (!hallExists)
            throw new NotFoundException("Dvorana nije pronađena.");

        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.HallPhotos
            .AsNoTracking()
            .Where(p => p.HallId == hallId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(p => new HallPhotoResponse { Id = p.Id, HallId = p.HallId, SortOrder = p.SortOrder, ImageUrl = p.ImageUrl })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<HallPhotoResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<HallPhotoResponse> GetByIdAsync(int hallId, int photoId, CancellationToken cancellationToken = default)
    {
        var photo = await _db.HallPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.HallId == hallId && p.Id == photoId, cancellationToken);

        if (photo is null)
            throw new NotFoundException("Slika nije pronađena.");

        return new HallPhotoResponse { Id = photo.Id, HallId = photo.HallId, SortOrder = photo.SortOrder, ImageUrl = photo.ImageUrl };
    }

    public async Task<HallPhotoResponse> CreateAsync(int hallId, CreateHallPhotoRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var hallExists = await _db.Halls.AsNoTracking().AnyAsync(h => h.Id == hallId, cancellationToken);
        if (!hallExists)
            throw new NotFoundException("Dvorana nije pronađena.");

        var entity = new HallPhoto
        {
            HallId = hallId,
            SortOrder = request.SortOrder,
            ImageUrl = request.ImageUrl.Trim(),
        };

        _db.HallPhotos.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new HallPhotoResponse { Id = entity.Id, HallId = entity.HallId, SortOrder = entity.SortOrder, ImageUrl = entity.ImageUrl };
    }

    public async Task<HallPhotoResponse> UpdateAsync(
        int hallId,
        int photoId,
        UpdateHallPhotoRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var entity = await _db.HallPhotos.FirstOrDefaultAsync(p => p.HallId == hallId && p.Id == photoId, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Slika nije pronađena.");

        entity.SortOrder = request.SortOrder;
        entity.ImageUrl = request.ImageUrl.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return new HallPhotoResponse { Id = entity.Id, HallId = entity.HallId, SortOrder = entity.SortOrder, ImageUrl = entity.ImageUrl };
    }

    public async Task DeleteAsync(int hallId, int photoId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.HallPhotos.FirstOrDefaultAsync(p => p.HallId == hallId && p.Id == photoId, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Slika nije pronađena.");

        _db.HallPhotos.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}


