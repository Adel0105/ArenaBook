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

public sealed class PlatformSettingEntryService : IPlatformSettingEntryService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreatePlatformSettingEntryRequest> _createValidator;
    private readonly IValidator<UpdatePlatformSettingEntryRequest> _updateValidator;

    public PlatformSettingEntryService(
        ArenaBookDbContext db,
        IValidator<CreatePlatformSettingEntryRequest> createValidator,
        IValidator<UpdatePlatformSettingEntryRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<PlatformSettingEntryResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        string? key,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.PlatformSettingEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(key))
        {
            var k = key.Trim();
            query = query.Where(x => x.SettingKey == k);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.SettingKey.Contains(term) || x.SettingValue.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.SettingKey)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new PlatformSettingEntryResponse
            {
                Id = x.Id,
                SettingKey = x.SettingKey,
                SettingValue = x.SettingValue,
                UpdatedUtc = x.UpdatedUtc,
            })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<PlatformSettingEntryResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<PlatformSettingEntryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PlatformSettingEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Postavka platforme nije pronađena.");

        return new PlatformSettingEntryResponse
        {
            Id = entity.Id,
            SettingKey = entity.SettingKey,
            SettingValue = entity.SettingValue,
            UpdatedUtc = entity.UpdatedUtc,
        };
    }

    public async Task<PlatformSettingEntryResponse> CreateAsync(
        CreatePlatformSettingEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var key = request.SettingKey.Trim();
        var value = request.SettingValue.Trim();

        var exists = await _db.PlatformSettingEntries.AnyAsync(x => x.SettingKey == key, cancellationToken);
        if (exists)
            throw new ConflictException("Postavka sa istim ključem već postoji.");

        var entity = new PlatformSettingEntry
        {
            SettingKey = key,
            SettingValue = value,
            UpdatedUtc = DateTime.UtcNow,
        };

        _db.PlatformSettingEntries.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new PlatformSettingEntryResponse
        {
            Id = entity.Id,
            SettingKey = entity.SettingKey,
            SettingValue = entity.SettingValue,
            UpdatedUtc = entity.UpdatedUtc,
        };
    }

    public async Task<PlatformSettingEntryResponse> UpdateAsync(
        int id,
        UpdatePlatformSettingEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var entity = await _db.PlatformSettingEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Postavka platforme nije pronađena.");

        var key = request.SettingKey.Trim();
        var value = request.SettingValue.Trim();

        var exists = await _db.PlatformSettingEntries.AnyAsync(x => x.Id != id && x.SettingKey == key, cancellationToken);
        if (exists)
            throw new ConflictException("Postavka sa istim ključem već postoji.");

        entity.SettingKey = key;
        entity.SettingValue = value;
        entity.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new PlatformSettingEntryResponse
        {
            Id = entity.Id,
            SettingKey = entity.SettingKey,
            SettingValue = entity.SettingValue,
            UpdatedUtc = entity.UpdatedUtc,
        };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PlatformSettingEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Postavka platforme nije pronađena.");

        _db.PlatformSettingEntries.Remove(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException)
        {
            throw new ConflictException("Brisanje nije moguće.");
        }
    }
}


