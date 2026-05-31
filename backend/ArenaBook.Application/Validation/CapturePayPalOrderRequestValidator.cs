using ArenaBook.Application.Contracts.Payments;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CapturePayPalOrderRequestValidator : AbstractValidator<CapturePayPalOrderRequest>
{
    public CapturePayPalOrderRequestValidator()
    {
        RuleFor(x => x.PayPalOrderId).NotEmpty().MaximumLength(64);
    }
}

