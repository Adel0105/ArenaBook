using ArenaBook.Application.Contracts.Halls;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreateHallPhotoRequestValidator : AbstractValidator<CreateHallPhotoRequest>
{
    public CreateHallPhotoRequestValidator()
    {
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(2048);
    }
}


