using System.Collections.Frozen;
using System.Text;
using FluentValidation.Results;

namespace SlotBook.SharedKernel.Application.Validation;

/// <summary>
/// The one map from FluentValidation error codes to our field codes (AD-13), and the field-name rules.
/// Field codes share the <c>errors.{code}</c> namespace with refusal codes.
/// </summary>
public static class ValidationCodes
{
    public const string Required = "required";
    public const string TooLong = "too_long";
    public const string TooShort = "too_short";
    public const string InvalidLength = "invalid_length";
    public const string OutOfRange = "out_of_range";
    public const string InvalidFormat = "invalid_format";
    public const string InvalidValue = "invalid_value";

    private static readonly FrozenDictionary<string, string> _map = new Dictionary<string, string>
    {
        ["NotEmptyValidator"] = Required,
        ["NotNullValidator"] = Required,
        ["MaximumLengthValidator"] = TooLong,
        ["MinimumLengthValidator"] = TooShort,
        ["LengthValidator"] = InvalidLength,
        ["ExactLengthValidator"] = InvalidLength,
        ["GreaterThanValidator"] = OutOfRange,
        ["GreaterThanOrEqualValidator"] = OutOfRange,
        ["LessThanValidator"] = OutOfRange,
        ["LessThanOrEqualValidator"] = OutOfRange,
        ["InclusiveBetweenValidator"] = OutOfRange,
        ["ExclusiveBetweenValidator"] = OutOfRange,
        ["RegularExpressionValidator"] = InvalidFormat,
        ["EmailValidator"] = InvalidFormat,
        ["EnumValidator"] = InvalidValue,
        ["StringEnumValidator"] = InvalidValue,
        ["EqualValidator"] = InvalidValue,
        ["NotEqualValidator"] = InvalidValue,
        ["EmptyValidator"] = InvalidValue,
        ["NullValidator"] = InvalidValue,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Gets every code this class defines.</summary>
    public static IReadOnlyCollection<string> All { get; } =
        [Required, TooLong, TooShort, InvalidLength, OutOfRange, InvalidFormat, InvalidValue];

    /// <summary>Returns whether <paramref name="code"/> is non-empty snake_case (lowercase letters, digits, underscores).</summary>
    /// <param name="code">The candidate code.</param>
    /// <returns><see langword="true"/> for a valid code.</returns>
    public static bool IsCode(string? code)
    {
        if (string.IsNullOrEmpty(code) || code[0] is < 'a' or > 'z' || code[^1] == '_')
        {
            return false;
        }

        for (var i = 0; i < code.Length; i++)
        {
            var c = code[i];
            if (!(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_') || (c == '_' && code[i - 1] == '_'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Maps a FluentValidation error code to ours. Our own snake_case codes pass through; anything else throws.</summary>
    /// <param name="errorCode">FluentValidation's <c>ErrorCode</c>.</param>
    /// <returns>The snake_case code.</returns>
    /// <exception cref="InvalidOperationException">The code is unmapped (e.g. a <c>Must</c> rule without <c>WithErrorCode</c>).</exception>
    public static string Resolve(string? errorCode)
    {
        if (errorCode is not null && _map.TryGetValue(errorCode, out var mapped))
        {
            return mapped;
        }

        if (IsCode(errorCode))
        {
            return errorCode!;
        }

        throw new InvalidOperationException(
            $"Validation error code '{errorCode}' has no mapping. Use WithErrorCode(...) with a snake_case code.");
    }

    /// <summary>Converts a FluentValidation property path to camelCase segments: <c>Items[0].Name</c> → <c>items[0].name</c>.</summary>
    /// <param name="propertyName">The property path.</param>
    /// <returns>The camelCase path.</returns>
    public static string ToFieldName(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        var builder = new StringBuilder(propertyName.Length);
        var segmentStart = true;
        foreach (var c in propertyName)
        {
            builder.Append(segmentStart ? char.ToLowerInvariant(c) : c);
            segmentStart = c == '.';
        }

        return builder.ToString();
    }

    /// <summary>Groups failures into codes per camelCase field, de-duplicated, in order of first appearance.</summary>
    /// <param name="failures">The failures.</param>
    /// <returns>The errors dictionary.</returns>
    public static Dictionary<string, string[]> ToErrors(IEnumerable<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        return failures
            .GroupBy(f => ToFieldName(f.PropertyName), StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => Resolve(f.ErrorCode)).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
    }
}
