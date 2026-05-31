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

public sealed class HallService : IHallService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateHallRequest> _createValidator;
    private readonly IValidator<UpdateHallRequest> _updateValidator;

    public HallService(
        ArenaBookDbContext db,
        IValidator<CreateHallRequest> createValidator,
        IValidator<UpdateHallRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<HallListItemResponse>> GetPagedAsync(
        PageRequest page,
        HallListQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var halls = _db.Halls.AsNoTracking();

        if (query.CityId.HasValue)
            halls = halls.Where(h => h.CityId == query.CityId.Value);

        if (query.CountryId.HasValue)
            halls = halls.Where(h => h.City.CountryId == query.CountryId.Value);

        if (query.IsActive.HasValue)
            halls = halls.Where(h => h.IsActive == query.IsActive.Value);

        if (query.MinCapacityPeople.HasValue)
            halls = halls.Where(h => h.CapacityPeople >= query.MinCapacityPeople.Value);

        if (query.MaxCapacityPeople.HasValue)
            halls = halls.Where(h => h.CapacityPeople <= query.MaxCapacityPeople.Value);

        if (query.MinPricePerHourCoins.HasValue)
            halls = halls.Where(h => h.PricePerHourCoins >= query.MinPricePerHourCoins.Value);

        if (query.MaxPricePerHourCoins.HasValue)
            halls = halls.Where(h => h.PricePerHourCoins <= query.MaxPricePerHourCoins.Value);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            halls = halls.Where(h =>
                h.Name.Contains(term) ||
                h.StreetAddress.Contains(term) ||
                h.City.Name.Contains(term));
        }

        var projected = halls
            .OrderBy(h => h.Name)
            .Select(h => new HallListItemResponse
            {
                Id = h.Id,
                Name = h.Name,
                CityId = h.CityId,
                CityName = h.City.Name,
                CountryId = h.City.CountryId,
                CountryName = h.City.Country.Name,
                StreetAddress = h.StreetAddress,
                CapacityPeople = h.CapacityPeople,
                PricePerHourCoins = h.PricePerHourCoins,
                ContactPhone = h.ContactPhone,
                IsActive = h.IsActive,
                PrimaryImageUrl = h.Photos
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Id)
                    .Select(p => p.ImageUrl)
                    .FirstOrDefault(),
            });

        var total = await projected.CountAsync(cancellationToken);
        var items = await projected.Skip(skip).Take(normalizedPageSize).ToListAsync(cancellationToken);

        return new PagedListResponse<HallListItemResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<HallDetailsResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var hall = await _db.Halls
            .AsNoTracking()
            .Where(h => h.Id == id)
            .Select(h => new HallDetailsResponse
            {
                Id = h.Id,
                Name = h.Name,
                CityId = h.CityId,
                CityName = h.City.Name,
                CountryId = h.City.CountryId,
                CountryName = h.City.Country.Name,
                StreetAddress = h.StreetAddress,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
                CapacityPeople = h.CapacityPeople,
                PricePerHourCoins = h.PricePerHourCoins,
                ContactPhone = h.ContactPhone,
                IsActive = h.IsActive,
                CreatedUtc = h.CreatedUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (hall is null)
            throw new NotFoundException("Dvorana nije pronađena.");

        return hall;
    }

    public async Task<HallDetailsResponse> CreateAsync(CreateHallRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var cityExists = await _db.Cities.AnyAsync(c => c.Id == request.CityId, cancellationToken);
        if (!cityExists)
            throw new NotFoundException("Grad nije pronađen.");

        var entity = new Hall
        {
            Name = request.Name.Trim(),
            CityId = request.CityId,
            StreetAddress = request.StreetAddress.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CapacityPeople = request.CapacityPeople,
            PricePerHourCoins = request.PricePerHourCoins,
            ContactPhone = request.ContactPhone.Trim(),
            IsActive = request.IsActive,
            CreatedUtc = DateTime.UtcNow,
        };

        _db.Halls.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<HallDetailsResponse> UpdateAsync(int id, UpdateHallRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var entity = await _db.Halls.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Dvorana nije pronađena.");

        var cityExists = await _db.Cities.AnyAsync(c => c.Id == request.CityId, cancellationToken);
        if (!cityExists)
            throw new NotFoundException("Grad nije pronađen.");

        entity.Name = request.Name.Trim();
        entity.CityId = request.CityId;
        entity.StreetAddress = request.StreetAddress.Trim();
        entity.Latitude = request.Latitude;
        entity.Longitude = request.Longitude;
        entity.CapacityPeople = request.CapacityPeople;
        entity.PricePerHourCoins = request.PricePerHourCoins;
        entity.ContactPhone = request.ContactPhone.Trim();
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Halls.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Dvorana nije pronađena.");

        _db.Halls.Remove(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException)
        {
            throw new ConflictException("Brisanje nije moguće jer postoje povezani zapisi (npr. termini/recenzije).");
        }
    }
}


