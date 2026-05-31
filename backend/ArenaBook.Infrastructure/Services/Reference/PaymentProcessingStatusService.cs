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

public sealed class PaymentProcessingStatusService : IPaymentProcessingStatusService
{
    private readonly ArenaBookDbContext _db;
    private readonly IValidator<CreatePaymentProcessingStatusRequest> _createValidator;
    private readonly IValidator<UpdatePaymentProcessingStatusRequest> _updateValidator;

    public PaymentProcessingStatusService(
        ArenaBookDbContext db,
        IValidator<CreatePaymentProcessingStatusRequest> createValidator,
        IValidator<UpdatePaymentProcessingStatusRequest> updateValidator)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedListResponse<PaymentProcessingStatusResponse>> GetPagedAsync(
        PageRequest page,
        string? q,
        string? code,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page.GetNormalizedPage();
        var normalizedPageSize = page.GetNormalizedPageSize();
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var query = _db.PaymentProcessingStatuses.AsNoTracking();

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
            .Select(x => new PaymentProcessingStatusResponse { Id = x.Id, Code = x.Code, DisplayName = x.DisplayName })
            .ToListAsync(cancellationToken);

        return new PagedListResponse<PaymentProcessingStatusResponse>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = total,
        };
    }

    public async Task<PaymentProcessingStatusResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentProcessingStatuses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Status obrade plaćanja nije pronađen.");

        return new PaymentProcessingStatusResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task<PaymentProcessingStatusResponse> CreateAsync(
        CreatePaymentProcessingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var code = request.Code.Trim();
        var displayName = request.DisplayName.Trim();

        var codeExists = await _db.PaymentProcessingStatuses.AnyAsync(x => x.Code == code, cancellationToken);
        if (codeExists)
            throw new ConflictException("Status sa istim kodom već postoji.");

        var entity = new PaymentProcessingStatus { Code = code, DisplayName = displayName };
        _db.PaymentProcessingStatuses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentProcessingStatusResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task<PaymentProcessingStatusResponse> UpdateAsync(
        int id,
        UpdatePaymentProcessingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ArenaBook.Application.Common.Exceptions.ValidationException("Validacija nije prošla.", validation.ToErrorDictionary());

        var entity = await _db.PaymentProcessingStatuses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Status obrade plaćanja nije pronađen.");

        var code = request.Code.Trim();
        var displayName = request.DisplayName.Trim();

        var codeExists = await _db.PaymentProcessingStatuses.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken);
        if (codeExists)
            throw new ConflictException("Status sa istim kodom već postoji.");

        entity.Code = code;
        entity.DisplayName = displayName;
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentProcessingStatusResponse { Id = entity.Id, Code = entity.Code, DisplayName = entity.DisplayName };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentProcessingStatuses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new NotFoundException("Status obrade plaćanja nije pronađen.");

        _db.PaymentProcessingStatuses.Remove(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException)
        {
            throw new ConflictException("Brisanje nije moguće jer postoje povezani zapisi (npr. uplate).");
        }
    }
}


