using ArenaBook.Application.Abstractions.Reference;
using ArenaBook.Application.Common.Exceptions;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;
using ArenaBook.Domain.Entities;
using ArenaBook.Infrastructure.Persistence;
using ArenaBook.Infrastructure.Validation;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArenaBook.Infrastructure.Services.Reference;

public sealed class CityService : ICityService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateCityRequest> _createValidator;
    private readonly IValidator<UpdateCityRequest> _updateValidator;

    public CityService(
        ArenaBookDbContext db,
        IValidator<CreateCityRequest> createValidator,
        IValidator<UpdateCityRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<CityResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        int? countryId,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.Cities.AsNoTracking();

        if (countryId.HasValue)
            query = query.Where(x => x.CountryId == countryId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Name.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new CityResponse { Id = x.Id, CountryId = x.CountryId, Name = x.Name })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<CityResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<CityResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _db.Cities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (city is null)
            throw new NotFoundException("Grad nije pronađen.");

        return new CityResponse { Id = city.Id, CountryId = city.CountryId, Name = city.Name };
    }

    public async Task<CityResponse> CreateAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var countryExists = await _db.Countries.AnyAsync(x => x.Id == request.CountryId, cancellationToken);
        if (!countryExists)
            throw new NotFoundException("Država nije pronađena.");

        var name = request.Name.Trim();
        var exists = await _db.Cities.AnyAsync(
            x => x.CountryId == request.CountryId && x.Name == name,
            cancellationToken);
        if (exists)
            throw new ConflictException("Grad sa istim nazivom već postoji u ovoj državi.");

        var entity = new City { CountryId = request.CountryId, Name = name };
        _db.Cities.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new CityResponse { Id = entity.Id, CountryId = entity.CountryId, Name = entity.Name };
    }

    public async Task<CityResponse> UpdateAsync(int id, UpdateCityRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var entity = await _db.Cities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Grad nije pronađen.");

        var countryExists = await _db.Countries.AnyAsync(x => x.Id == request.CountryId, cancellationToken);
        if (!countryExists)
            throw new NotFoundException("Država nije pronađena.");

        var name = request.Name.Trim();
        var exists = await _db.Cities.AnyAsync(
            x => x.Id != id && x.CountryId == request.CountryId && x.Name == name,
            cancellationToken);
        if (exists)
            throw new ConflictException("Grad sa istim nazivom već postoji u ovoj državi.");

        entity.CountryId = request.CountryId;
        entity.Name = name;
        await _db.SaveChangesAsync(cancellationToken);

        return new CityResponse { Id = entity.Id, CountryId = entity.CountryId, Name = entity.Name };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Cities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Grad nije pronađen.");

        _db.Cities.Remove(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException)
        {
            throw new ConflictException("Brisanje nije moguće jer postoje povezani zapisi (npr. dvorane).");
        }
    }
}


