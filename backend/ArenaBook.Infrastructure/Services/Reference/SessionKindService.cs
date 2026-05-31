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

public sealed class SessionKindService : ISessionKindService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateSessionKindRequest> _createValidator;
    private readonly IValidator<UpdateSessionKindRequest> _updateValidator;

    public SessionKindService(
        ArenaBookDbContext db,
        IValidator<CreateSessionKindRequest> createValidator,
        IValidator<UpdateSessionKindRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<SessionKindResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        string? code,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.SessionKinds.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(code))
        {
            var c = code.Trim();
            query = query.Where(x => x.Code == c);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.DisplayName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Code)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(x => new SessionKindResponse { Id = x.Id, Code = x.Code, DisplayName = x.DisplayName })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<SessionKindResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<SessionKindResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SessionKinds.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Vrsta termina nije pronađena.");

        return new SessionKindResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task<SessionKindResponse> CreateAsync(CreateSessionKindRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var code = request.Code.Trim();
        var displayName = request.DisplayName.Trim();

        var codeExists = await _db.SessionKinds.AnyAsync(x => x.Code == code, cancellationToken);
        if (codeExists)
            throw new ConflictException("Vrsta termina sa istim kodom već postoji.");

        var entity = new SessionKind { Code = code, DisplayName = displayName };
        _db.SessionKinds.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new SessionKindResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task<SessionKindResponse> UpdateAsync(int id, UpdateSessionKindRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var entity = await _db.SessionKinds.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Vrsta termina nije pronađena.");

        var code = request.Code.Trim();
        var displayName = request.DisplayName.Trim();

        var codeExists = await _db.SessionKinds.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken);
        if (codeExists)
            throw new ConflictException("Vrsta termina sa istim kodom već postoji.");

        entity.Code = code;
        entity.DisplayName = displayName;
        await _db.SaveChangesAsync(cancellationToken);

        return new SessionKindResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SessionKinds.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Vrsta termina nije pronađena.");

        _db.SessionKinds.Remove(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException)
        {
            throw new ConflictException("Brisanje nije moguće jer postoje povezani zapisi (npr. termini).");
        }
    }
}


