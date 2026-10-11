using System.Xml.Linq;

namespace SlotBook.ArchitectureTests;

/// <summary>A project and its direct references, as the csproj scan sees them.</summary>
internal sealed record ProjectNode(
    string Name,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> PackageReferences,
    IReadOnlyList<string> FrameworkReferences)
{
    public static ProjectNode Load(string csprojPath)
    {
        var project = XDocument.Load(csprojPath).Root!;

        List<string> Attributes(string element, string attribute) =>
            [.. project.Descendants(element).Select(node => (string?)node.Attribute(attribute)).OfType<string>()];

        return new ProjectNode(
            Path.GetFileNameWithoutExtension(csprojPath),
            [.. Attributes("ProjectReference", "Include").Select(path => Path.GetFileNameWithoutExtension(path.Replace('\\', '/')))],
            Attributes("PackageReference", "Include"),
            Attributes("FrameworkReference", "Include"));
    }
}
