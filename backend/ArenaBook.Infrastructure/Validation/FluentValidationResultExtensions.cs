using FluentValidation.Results;

namespace ArenaBook.Infrastructure.Validation;

internal static class FluentValidationResultExtensions
{
    public static IReadOnlyDictionary<string, string[]> ToErrorDictionary(this ValidationResult result)
    {
        IReadOnlyDictionary<string, string[]> dict = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return dict;
    }
}


