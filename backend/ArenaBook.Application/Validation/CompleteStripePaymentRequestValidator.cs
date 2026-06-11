using ArenaBook.Application.Contracts.Payments;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CompleteStripePaymentRequestValidator
    : AbstractValidator<CompleteStripePaymentRequest>
{
    public CompleteStripePaymentRequestValidator()
    {
        RuleFor(x => x.PaymentIntentId)
            .NotEmpty()
            .WithMessage("PaymentIntentId je obavezan.");
    }
}
