using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class ProjectReferenceTests
{
    [Fact]
    public void TheRepositoryProjectsFollowTheRules()
    {
        var backend = Path.Combine(SlotBookArchitecture.RepoRoot, "src", "backend");
        var projects = Directory.EnumerateFiles(backend, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(backend, path).StartsWith("tests" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Select(ProjectNode.Load)
            .ToList();

        projects.ShouldNotBeEmpty();
        ProjectReferenceRules.Violations(projects).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("SlotBook.SharedKernel.Domain", "SlotBook.SharedKernel.Infrastructure")]
    [InlineData("SlotBook.SharedKernel.Contracts", "SlotBook.SharedKernel.Domain")]
    [InlineData("SlotBook.ServiceDefaults", "SlotBook.SharedKernel.Contracts")]
    [InlineData("SlotBook.Modules.X.Domain", "SlotBook.Modules.X.Application")]
    [InlineData("SlotBook.Modules.X.Application", "SlotBook.Modules.Y.Application")]
    [InlineData("SlotBook.Modules.X.Application", "SlotBook.Modules.X.Infrastructure")]
    [InlineData("SlotBook.Modules.X.Infrastructure", "SlotBook.Modules.Y.Contracts")]
    [InlineData("SlotBook.Modules.X.Infrastructure", "SlotBook.Modules.X.Domain")]
    [InlineData("SlotBook.Modules.X", "SlotBook.Modules.Y")]
    [InlineData("SlotBook.SharedKernel.Application", "SlotBook.SharedKernel.Infrastructure")]
    public void ADisallowedReferenceFailsWithTheRuleName(string from, string to)
    {
        var violations = ProjectReferenceRules.Violations([new ProjectNode(from, [to], [], [])]);

        violations.ShouldBe([$"{ProjectReferenceRules.References}: {from} must not reference {to}"]);
    }

    [Theory]
    [InlineData("SlotBook.Api", "SlotBook.ServiceDefaults")]
    [InlineData("SlotBook.Api", "SlotBook.SharedKernel.Infrastructure")]
    [InlineData("SlotBook.Api", "SlotBook.Modules.X.Infrastructure")]
    [InlineData("SlotBook.Api", "SlotBook.Modules.X")]
    [InlineData("SlotBook.SharedKernel.Infrastructure", "SlotBook.SharedKernel.Application")]
    [InlineData("SlotBook.SharedKernel.Application", "SlotBook.SharedKernel.Domain")]
    [InlineData("SlotBook.SharedKernel.Domain", "SlotBook.SharedKernel.Contracts")]
    [InlineData("SlotBook.Modules.X.Infrastructure", "SlotBook.Modules.X.Application")]
    [InlineData("SlotBook.Modules.X.Infrastructure", "SlotBook.SharedKernel.Infrastructure")]
    [InlineData("SlotBook.Modules.X.Application", "SlotBook.Modules.X.Domain")]
    [InlineData("SlotBook.Modules.X.Application", "SlotBook.Modules.Y.Contracts")]
    [InlineData("SlotBook.Modules.X.Domain", "SlotBook.SharedKernel.Domain")]
    [InlineData("SlotBook.Modules.X", "SlotBook.SharedKernel.Infrastructure")]
    [InlineData("SlotBook.Modules.X.Contracts", "SlotBook.SharedKernel.Contracts")]
    public void AnAllowedReferencePasses(string from, string to) =>
        ProjectReferenceRules.Violations([new ProjectNode(from, [to], [], [])]).ShouldBeEmpty();

    [Theory]
    [InlineData("SlotBook.SharedKernel.Contracts", "MongoDB.Driver")]
    [InlineData("SlotBook.SharedKernel.Domain", "MassTransit")]
    [InlineData("SlotBook.Modules.X.Domain", "MongoDB.Bson")]
    [InlineData("SlotBook.Modules.X.Contracts", "Microsoft.AspNetCore.Mvc.Core")]
    public void AnInfrastructurePackageInAPureProjectFailsWithTheRuleName(string project, string package)
    {
        var violations = ProjectReferenceRules.Violations([new ProjectNode(project, [], [package], [])]);

        violations.ShouldBe([$"{ProjectReferenceRules.PureProjects}: {project} must not reference the package {package}"]);
    }

    [Theory]
    [InlineData("SlotBook.Modules.X.Domain")]
    [InlineData("SlotBook.SharedKernel.Contracts")]
    public void TheAspNetCoreFrameworkInAPureProjectFailsWithTheRuleName(string project)
    {
        var violations = ProjectReferenceRules.Violations([new ProjectNode(project, [], [], ["Microsoft.AspNetCore.App"])]);

        violations.ShouldBe([$"{ProjectReferenceRules.PureProjects}: {project} must not reference the framework Microsoft.AspNetCore.App"]);
    }

    [Fact]
    public void InfrastructureProjectsMayUseInfrastructurePackages() =>
        ProjectReferenceRules.Violations([new ProjectNode("SlotBook.Modules.X.Infrastructure", [], ["MongoDB.Driver"], ["Microsoft.AspNetCore.App"])])
            .ShouldBeEmpty();
}
