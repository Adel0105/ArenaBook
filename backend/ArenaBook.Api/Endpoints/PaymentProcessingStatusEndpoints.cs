using ArenaBook.Application.Abstractions.Reference;
using ArenaBook.Application.Authorization;
using ArenaBook.Application.Common.Paging;
using ArenaBook.Application.Contracts.Reference;

namespace ArenaBook.Api.Endpoints;

public static class PaymentProcessingStatusEndpoints
{
    public static WebApplication MapPaymentProcessingStatusEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/reference/payment-processing-statuses")
            .WithTags("Reference - PaymentProcessingStatuses");

        group.MapGet("/", GetPagedAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapGet("/{id:int}", GetByIdAsync)
            .RequireAuthorization(AuthPolicies.CatalogRead);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapPut("/{id:int}", UpdateAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        group.MapDelete("/{id:int}", DeleteAsync)
            .RequireAuthorization(AuthPolicies.AdministratorOnly);

        return app;
    }

    private static Task<PagedListResponse<PaymentProcessingStatusResponse>> GetPagedAsync(
        IPaymentProcessingStatusService service,
        int page = PageRequest.DefaultPage,
        int pageSize = PageRequest.DefaultPageSize,
        string? q = null,
        string? code = null,
        CancellationToken cancellationToken = default)
    {
        return service.GetPagedAsync(
            new PageRequest { Page = page, PageSize = pageSize },
            q,
            code,
            cancellationToken);
    }

    private static Task<PaymentProcessingStatusResponse> GetByIdAsync(
        IPaymentProcessingStatusService service,
        int id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }

    private static Task<PaymentProcessingStatusResponse> CreateAsync(
        IPaymentProcessingStatusService service,
        CreatePaymentProcessingStatusRequest request,
        CancellationToken cancellationToken)
    {
        return service.CreateAsync(request, cancellationToken);
    }

    private static Task<PaymentProcessingStatusResponse> UpdateAsync(
        IPaymentProcessingStatusService service,
        int id,
        UpdatePaymentProcessingStatusRequest request,
        CancellationToken cancellationToken)
    {
        return service.UpdateAsync(id, request, cancellationToken);
    }

    private static async Task<IResult> DeleteAsync(
        IPaymentProcessingStatusService service,
        int id,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}


