using SlotBook.SharedKernel.Application.Validation;

namespace SlotBook.SharedKernel.Application;

#pragma warning disable CA2225 // the implicit conversions are the API: `return value;` and `return Result.Refused(code);`

/// <summary>Either the value of a successful command or the code of a refusal.</summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T? _value;

    private Result(T? value, string? code)
    {
        _value = value;
        Code = code;
    }

    /// <summary>Gets a value indicating whether the request was refused.</summary>
    public bool IsRefused => Code is not null;

    /// <summary>Gets the refusal code, or <see langword="null"/> on success.</summary>
    public string? Code { get; }

    /// <summary>Gets the value. Throws when the result is a refusal.</summary>
    public T Value => IsRefused
        ? throw new InvalidOperationException($"The result was refused with '{Code}'.")
        : _value!;

    public static implicit operator Result<T>(T value) => new(value, null);

    public static implicit operator Result<T>(Refusal refusal) => new(default, refusal.Code);

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    public bool Equals(Result<T> other) =>
        Code == other.Code && EqualityComparer<T?>.Default.Equals(_value, other._value);

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Code, _value);
}

/// <summary>Entry point for creating refusals.</summary>
public static class Result
{
    /// <summary>Refuses the request with <paramref name="code"/>; the API answers 422 (404 for <c>not_found</c>).</summary>
    /// <param name="code">A non-empty snake_case code.</param>
    /// <returns>A refusal that converts implicitly to any <see cref="Result{T}"/>.</returns>
    public static Refusal Refused(string code)
    {
        if (!ValidationCodes.IsCode(code))
        {
            throw new ArgumentException("A refusal code must be non-empty snake_case.", nameof(code));
        }

        return new Refusal(code);
    }
}
