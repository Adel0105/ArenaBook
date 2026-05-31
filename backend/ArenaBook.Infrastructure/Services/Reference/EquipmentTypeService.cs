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

public sealed class EquipmentTypeService : IEquipmentTypeService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateEquipmentTypeRequest> _createValidator;
    private readonly IValidator<UpdateEquipmentTypeRequest> _updateValidator;

    public EquipmentTypeService(
        ArenaBookDbContext db,
        IValidator<CreateEquipmentTypeRequest> createValidator,
        IValidator<UpdateEquipmentTypeRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<EquipmentTypeResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.EquipmentTypes.AsNoTracking();
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
            .Select(x => new EquipmentTypeResponse { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<EquipmentTypeResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<EquipmentTypeResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.EquipmentTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Tip opreme nije pronađen.");

        return new EquipmentTypeResponse { Id = entity.Id, Name = entity.Name };
    }

    public async Task<EquipmentTypeResponse> CreateAsync(CreateEquipmentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var name = request.Name.Trim();
        var exists = await _db.EquipmentTypes.AnyAsync(x => x.Name == name, cancellationToken);
        if (exists)
            throw new ConflictException("Tip opreme sa istim nazivom već postoji.");

        var entity = new EquipmentType { Name = name };
        _db.EquipmentTypes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new EquipmentTypeResponse { Id = entity.Id, Name = entity.Name };
    }

    public async Task<EquipmentTypeResponse> UpdateAsync(int id, UpdateEquipmentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException(
                "Validacija nije prošla.",
                validation.ToErrorDictionary());

        var entity = await _db.EquipmentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Tip opreme nije pronađen.");

        var name = request.Name.Trim();
        var exists = await _db.EquipmentTypes.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken);
        if (exists)
            throw new ConflictException("Tip opreme sa istim nazivom već postoji.");

        entity.Name = name;
        await _db.SaveChangesAsync(cancellationToken);

        return new EquipmentTypeResponse { Id = entity.Id, Name = entity.Name };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.EquipmentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Tip opreme nije pronađen.");

        _db.EquipmentTypes.Remove(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException)
        {
            throw new ConflictException("Brisanje nije moguće jer postoje povezani zapisi (npr. oprema dvorane).");
        }
    }
}


