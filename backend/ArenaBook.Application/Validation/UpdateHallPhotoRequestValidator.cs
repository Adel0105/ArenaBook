using ArenaBook.Application.Contracts.Halls;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class UpdateHallPhotoRequestValidator : AbstractValidator<UpdateHallPhotoRequest>
{
    public UpdateHallPhotoRequestValidator()
    {
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(2048);
    }
}


