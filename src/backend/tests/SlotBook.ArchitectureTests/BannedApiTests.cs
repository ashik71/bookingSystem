using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class BannedApiTests
{
    [Theory]
    [InlineData("P:System.DateTime.Now", "AD-14 wall clock")]
    [InlineData("P:System.DateTime.UtcNow", "AD-14 wall clock")]
    [InlineData("P:System.DateTime.Today", "AD-14 wall clock")]
    [InlineData("P:System.DateTimeOffset.Now", "AD-14 wall clock")]
    [InlineData("P:System.DateTimeOffset.UtcNow", "AD-14 wall clock")]
    [InlineData("P:System.TimeProvider.System", "AD-14 wall clock")]
    [InlineData("M:System.Guid.NewGuid()", "AD-9 Guid.NewGuid")]
    [InlineData("M:System.Guid.CreateVersion7()", "AD-9 Guid.NewGuid")]
    public void TheSymbolIsBannedUnderItsRuleName(string symbol, string ruleName)
    {
        var entries = ReadBannedSymbols();

        entries.ShouldContainKey(symbol);
        entries[symbol].ShouldStartWith(ruleName);
    }

    [Fact]
    public void TheBannedApiAnalyzerReadsTheRootFile()
    {
        var props = File.ReadAllText(Path.Combine(SlotBookArchitecture.RepoRoot, "Directory.Build.props"));
        var packages = File.ReadAllText(Path.Combine(SlotBookArchitecture.RepoRoot, "Directory.Packages.props"));

        props.ShouldContain("BannedSymbols.txt");
        packages.ShouldContain("Microsoft.CodeAnalysis.BannedApiAnalyzers");
    }

    private static Dictionary<string, string> ReadBannedSymbols() =>
        File.ReadAllLines(Path.Combine(SlotBookArchitecture.RepoRoot, "BannedSymbols.txt"))
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split(';', 2))
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
}
