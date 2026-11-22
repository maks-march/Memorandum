using FluentValidation.Results;

namespace Memorandum.Application.Common;

internal static class ValidationMapping
{
    /// <summary>Ошибки FluentValidation → <see cref="Error.Validation"/> с группировкой по полям.</summary>
    public static Error ToError(this ValidationResult result) =>
        Error.Validation(result.Errors
            .GroupBy(failure => string.IsNullOrWhiteSpace(failure.PropertyName) ? "general" : failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray()));
}
