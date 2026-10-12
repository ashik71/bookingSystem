namespace SlotBook.ServiceDefaults;

internal static class BuildInfo
{
    /// <summary>
    /// The build's commit SHA from the host's <c>RENDER_GIT_COMMIT</c> variable; <see langword="null"/>
    /// unless it is 7-40 lowercase hex characters, so nothing else is echoed or exported.
    /// </summary>
    internal static string? ReadCommit(string? value) =>
        value is { Length: >= 7 and <= 40 } && value.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'))
            ? value
            : null;
}
