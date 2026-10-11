namespace SlotBook.SharedKernel.Application;

/// <summary>A business refusal: a snake_case code and nothing else (AD-13). Converts to any <see cref="Result{T}"/>.</summary>
public readonly record struct Refusal
{
    internal Refusal(string code)
    {
        Code = code;
    }

    /// <summary>Gets the stable snake_case error code.</summary>
    public string Code { get; }
}
