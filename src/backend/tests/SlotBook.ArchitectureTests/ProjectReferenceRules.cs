namespace SlotBook.ArchitectureTests;

/// <summary>The AD-2 project-reference rules: allowed edges, and projects that must stay free of infrastructure.</summary>
internal static class ProjectReferenceRules
{
    public const string References = "AD-2 project references";
    public const string PureProjects = "AD-2 pure projects";

    private static readonly string[] _forbiddenPackagePrefixes = ["MongoDB.", "MassTransit", "Microsoft.AspNetCore."];

    public static IReadOnlyList<string> Violations(IEnumerable<ProjectNode> projects)
    {
        var violations = new List<string>();
        foreach (var project in projects)
        {
            var from = Kind.Parse(project.Name);
            foreach (var reference in project.ProjectReferences.Where(reference => !Allowed(from, Kind.Parse(reference))))
            {
                violations.Add($"{References}: {project.Name} must not reference {reference}");
            }

            if (from.IsPure)
            {
                violations.AddRange(project.PackageReferences
                    .Where(package => _forbiddenPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.Ordinal)))
                    .Select(package => $"{PureProjects}: {project.Name} must not reference the package {package}"));
                violations.AddRange(project.FrameworkReferences
                    .Where(framework => framework.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal))
                    .Select(framework => $"{PureProjects}: {project.Name} must not reference the framework {framework}"));
            }
        }

        return violations;
    }

    private static bool Allowed(Kind from, Kind to) => (from.Role, to.Role) switch
    {
        (Role.Api, Role.ServiceDefaults) => true,
        (Role.Api, Role.KernelInfrastructure) => true,
        (Role.Api, Role.ModuleInfrastructure) => true,
        (Role.Api, Role.SimpleModule) => true,
        (Role.KernelInfrastructure, Role.KernelApplication) => true,
        (Role.KernelApplication, Role.KernelDomain) => true,
        (Role.KernelApplication, Role.KernelContracts) => true,
        (Role.KernelDomain, Role.KernelContracts) => true,
        (Role.ModuleInfrastructure, Role.ModuleApplication) => from.Module == to.Module,
        (Role.ModuleInfrastructure, Role.KernelInfrastructure) => true,
        (Role.ModuleApplication, Role.ModuleDomain) => from.Module == to.Module,
        (Role.ModuleApplication, Role.ModuleContracts or Role.KernelContracts or Role.KernelApplication) => true,
        (Role.ModuleDomain, Role.KernelDomain) => true,
        (Role.SimpleModule, Role.KernelInfrastructure or Role.ModuleContracts or Role.KernelContracts) => true,
        (Role.ModuleContracts, Role.KernelContracts) => true,
        _ => false,
    };
}
