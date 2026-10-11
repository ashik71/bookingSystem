using Microsoft.AspNetCore.Mvc.ModelBinding;
using SlotBook.SharedKernel.Application.Validation;

namespace SlotBook.Api.Errors;

internal static class ModelBindingErrors
{
    private const string BodyField = "body";

    /// <summary>
    /// Turns model-binding failures (malformed JSON, wrong JSON types) into <c>{ field: ["invalid_format"] }</c>,
    /// never the framework's English messages (AD-13).
    /// </summary>
    public static ValidationFailedException ToException(ModelStateDictionary modelState)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            var path = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
            var field = path is "" or "$" ? BodyField : ValidationCodes.ToFieldName(path);
            errors[field] = [ValidationCodes.InvalidFormat];
        }

        return new ValidationFailedException(errors);
    }
}
