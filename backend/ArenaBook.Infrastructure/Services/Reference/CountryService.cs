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

public sealed class CountryService : ICountryService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateCountryRequest> _createValidator;
    private readonly IValidator<UpdateCountryRequest> _updateValidator;

    public CountryService(
        ArenaBookDbContext db,
        IValidator<CreateCountryRequest> createValidator,
        IValidator<UpdateCountryRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<CountryResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.Countries.AsNoTracking();
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
            .Select(x => new CountryResponse { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<CountryResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<CountryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var country = await _db.Countries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (country is null)
            throw new NotFoundException("Država nije pronađena.");

        return new CountryResponse { Id = country.Id, Name = country.Name };
    }

    public async Task<CountryResponse> CreateAsync(CreateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var name = request.Name.Trim();
        var exists = await _db.Countries.AnyAsync(x => x.Name == name, cancellationToken);
        if (exists)
            throw new ConflictException("Država sa istim nazivom već postoji.");

        var entity = new Country { Name = name };
        _db.Countries.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new CountryResponse { Id = entity.Id, Name = entity.Name };
    }

    public async Task<CountryResponse> UpdateAsync(int id, UpdateCountryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var entity = await _db.Countries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Država nije pronađena.");

        var name = request.Name.Trim();
        var exists = await _db.Countries.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken);
        if (exists)
            throw new ConflictException("Država sa istim nazivom već postoji.");

        entity.Name = name;
        await _db.SaveChangesAsync(cancellationToken);

        return new CountryResponse { Id = entity.Id, Name = entity.Name };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Countries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Država nije pronađena.");

        _db.Countries.Remove(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException)
        {
            throw new ConflictException("Brisanje nije moguće jer postoje povezani zapisi (npr. gradovi).");
        }
    }
}


