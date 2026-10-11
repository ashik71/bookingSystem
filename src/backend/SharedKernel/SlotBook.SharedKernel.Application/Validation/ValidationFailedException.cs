namespace SlotBook.SharedKernel.Application.Validation;

/// <summary>Thrown by the dispatcher when validators reject a request. The API maps it to 400 (AD-13).</summary>
public sealed class ValidationFailedException : Exception
{
    public ValidationFailedException(IReadOnlyDictionary<string, string[]> errors)
        : base(CommonErrors.ValidationFailed)
    {
        Errors = errors;
    }

    public ValidationFailedException()
        : this(new Dictionary<string, string[]>())
    {
    }

    public ValidationFailedException(string message)
        : this(new Dictionary<string, string[]>())
    {
    }

    public ValidationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>Gets the codes per camelCase field name.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
