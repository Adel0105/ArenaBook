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

public sealed class SessionLifecycleStatusService : ISessionLifecycleStatusService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreateSessionLifecycleStatusRequest> _createValidator;
    private readonly IValidator<UpdateSessionLifecycleStatusRequest> _updateValidator;

    public SessionLifecycleStatusService(
        ArenaBookDbContext db,
        IValidator<CreateSessionLifecycleStatusRequest> createValidator,
        IValidator<UpdateSessionLifecycleStatusRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<SessionLifecycleStatusResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        string? code,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.SessionLifecycleStatuses.AsNoTracking();

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
            .Select(x => new SessionLifecycleStatusResponse { Id = x.Id, Code = x.Code, DisplayName = x.DisplayName })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<SessionLifecycleStatusResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<SessionLifecycleStatusResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SessionLifecycleStatuses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Status termina nije pronađen.");

        return new SessionLifecycleStatusResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task<SessionLifecycleStatusResponse> CreateAsync(
        CreateSessionLifecycleStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var code = request.Code.Trim();
        var displayName = request.DisplayName.Trim();

        var codeExists = await _db.SessionLifecycleStatuses.AnyAsync(x => x.Code == code, cancellationToken);
        if (codeExists)
            throw new ConflictException("Status sa istim kodom već postoji.");

        var entity = new SessionLifecycleStatus { Code = code, DisplayName = displayName };
        _db.SessionLifecycleStatuses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new SessionLifecycleStatusResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task<SessionLifecycleStatusResponse> UpdateAsync(
        int id,
        UpdateSessionLifecycleStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var entity = await _db.SessionLifecycleStatuses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Status termina nije pronađen.");

        var code = request.Code.Trim();
        var displayName = request.DisplayName.Trim();

        var codeExists = await _db.SessionLifecycleStatuses.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken);
        if (codeExists)
            throw new ConflictException("Status sa istim kodom već postoji.");

        entity.Code = code;
        entity.DisplayName = displayName;
        await _db.SaveChangesAsync(cancellationToken);

        return new SessionLifecycleStatusResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SessionLifecycleStatuses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Status termina nije pronađen.");

        _db.SessionLifecycleStatuses.Remove(entity);
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


