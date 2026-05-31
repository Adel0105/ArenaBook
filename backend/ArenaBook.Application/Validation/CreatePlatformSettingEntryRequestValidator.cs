using ArenaBook.Application.Contracts.Reference;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class CreatePlatformSettingEntryRequestValidator : AbstractValidator<CreatePlatformSettingEntryRequest>
{
    public CreatePlatformSettingEntryRequestValidator()
    {
        RuleFor(x => x.SettingKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SettingValue).NotEmpty().MaximumLength(4000);
    }
}


