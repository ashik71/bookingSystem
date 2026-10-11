namespace SlotBook.SharedKernel.Domain;

/// <summary>
/// A business refusal that changes nothing. The API maps it to 422 with <see cref="Code"/>.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string code)
        : base(code)
    {
        Code = code;
    }

    public DomainException()
        : this("internal_error")
    {
    }

    public DomainException(string code, Exception innerException)
        : base(code, innerException)
    {
        Code = code;
    }

    /// <summary>Gets the stable snake_case error code (AD-13).</summary>
    public string Code { get; }
}
