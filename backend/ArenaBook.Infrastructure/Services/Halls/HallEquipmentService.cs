using ArenaBook.Application.Abstractions.Halls;
using ArenaBook.Application.Common.Exceptions;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Halls;
using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Persistence;
using ArenaBook.Infrastructure.Validation;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Halls;

public sealed class HallEquipmentService : IHallEquipmentService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateHallEquipmentRequest> _createValidator;
    private readonly IValidator<UpdateHallEquipmentRequest> _updateValidator;

    public HallEquipmentService(
        ArenaBookDbContext db,
        IValidator<CreateHallEquipmentRequest> createValidator,
        IValidator<UpdateHallEquipmentRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<HallEquipmentResponse>> GetPagedAsync(
        int hallId,
        PageRequest page,
        int? equipmentTypeId,
        CancellationToken cancellationToken = default)
    {
        var hallExists = await _db.Halls.AsNoTracking().AnyAsync(h => h.Id == hallId, cancellationToken);
        if (!hallExists)
            throw new NotFoundException("Dvorana nije pronađena.");

        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.HallEquipments.AsNoTracking().Where(x => x.HallId == hallId);
        if (equipmentTypeId.HasValue)
            query = query.Where(x => x.EquipmentTypeId == equipmentTypeId.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.EquipmentType.Name)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new HallEquipmentResponse
            {
                Id = x.Id,
                HallId = x.HallId,
                EquipmentTypeId = x.EquipmentTypeId,
                EquipmentTypeName = x.EquipmentType.Name,
                Quantity = x.Quantity,
            })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<HallEquipmentResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<HallEquipmentResponse> GetByIdAsync(int hallId, int linkId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.HallEquipments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.HallId == hallId && x.Id == linkId, cancellationToken);

        if (entity is null)
            throw new NotFoundException("Oprema nije pronađena.");

        var typeName = await _db.EquipmentTypes
            .AsNoTracking()
            .Where(t => t.Id == entity.EquipmentTypeId)
            .Select(t => t.Name)
            .FirstAsync(cancellationToken);

        return new HallEquipmentResponse
        {
            Id = entity.Id,
            HallId = entity.HallId,
            EquipmentTypeId = entity.EquipmentTypeId,
            EquipmentTypeName = typeName,
            Quantity = entity.Quantity,
        };
    }

    public async Task<HallEquipmentResponse> CreateAsync(int hallId, CreateHallEquipmentRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var hallExists = await _db.Halls.AsNoTracking().AnyAsync(h => h.Id == hallId, cancellationToken);
        if (!hallExists)
            throw new NotFoundException("Dvorana nije pronađena.");

        var typeExists = await _db.EquipmentTypes.AsNoTracking().AnyAsync(t => t.Id == request.EquipmentTypeId, cancellationToken);
        if (!typeExists)
            throw new NotFoundException("Tip opreme nije pronađen.");

        var entity = new HallEquipment
        {
            HallId = hallId,
            EquipmentTypeId = request.EquipmentTypeId,
            Quantity = request.Quantity,
        };

        _db.HallEquipments.Add(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException)
        {
            throw new ConflictException("Oprema ovog tipa je već dodana za ovu dvoranu.");
        }

        return await GetByIdAsync(hallId, entity.Id, cancellationToken);
    }

    public async Task<HallEquipmentResponse> UpdateAsync(
        int hallId,
        int linkId,
        UpdateHallEquipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var entity = await _db.HallEquipments.FirstOrDefaultAsync(x => x.HallId == hallId && x.Id == linkId, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Oprema nije pronađena.");

        entity.Quantity = request.Quantity;
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(hallId, entity.Id, cancellationToken);
    }

    public async Task DeleteAsync(int hallId, int linkId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.HallEquipments.FirstOrDefaultAsync(x => x.HallId == hallId && x.Id == linkId, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Oprema nije pronađena.");

        _db.HallEquipments.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}


