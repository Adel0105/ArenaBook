using ArenaBook.Application.Contracts.Reference;
using FluentValidation;

namespace ArenaBook.Application.Validation;

public sealed class UpdatePlatformSettingEntryRequestValidator : AbstractValidator<UpdatePlatformSettingEntryRequest>
{
    public UpdatePlatformSettingEntryRequestValidator()
    {
        RuleFor(x => x.SettingKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SettingValue).NotEmpty().MaximumLength(4000);
    }
}


