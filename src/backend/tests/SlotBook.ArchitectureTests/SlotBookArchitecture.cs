using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using Assembly = System.Reflection.Assembly;

namespace SlotBook.ArchitectureTests;

/// <summary>The loaded production code, and the repository layout the rules read.</summary>
internal static class SlotBookArchitecture
{
    private static readonly Lazy<Architecture> _production = new(() => Load(ProductionAssemblies()));
    private static readonly Lazy<Architecture> _fixtures = new(() => Load([typeof(SlotBookArchitecture).Assembly]));
    private static readonly Lazy<string> _repoRoot = new(FindRepoRoot);

    /// <summary>Every <c>SlotBook.*</c> production assembly; no test assembly.</summary>
    public static Architecture Production => _production.Value;

    /// <summary>The deliberate violations under <c>Fixtures/</c>; the rules run on them to prove they bite.</summary>
    public static Architecture Fixtures => _fixtures.Value;

    public static string RepoRoot => _repoRoot.Value;

    private static Architecture Load(IEnumerable<Assembly> assemblies) =>
        new ArchLoader().LoadAssemblies([.. assemblies]).Build();

    private static List<Assembly> ProductionAssemblies()
    {
        var api = typeof(Program).Assembly;
        var assemblies = new Dictionary<string, Assembly>(StringComparer.Ordinal) { [api.GetName().Name!] = api };
        var pending = new Stack<Assembly>([api]);
        while (pending.Count > 0)
        {
            foreach (var reference in pending.Pop().GetReferencedAssemblies())
            {
                if (reference.Name!.StartsWith("SlotBook.", StringComparison.Ordinal) && !assemblies.ContainsKey(reference.Name))
                {
                    var loaded = Assembly.Load(reference);
                    assemblies[reference.Name] = loaded;
                    pending.Push(loaded);
                }
            }
        }

        return [.. assemblies.Values];
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SlotBook.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("SlotBook.slnx not found above the test output.");
    }
}
