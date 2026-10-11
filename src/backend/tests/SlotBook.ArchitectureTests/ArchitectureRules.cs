using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace SlotBook.ArchitectureTests;

/// <summary>The AD-19 rules that run on ArchUnitNET. Every rule carries its name as the reason.</summary>
internal static class ArchitectureRules
{
    public const string DomainLayer = "AD-2 domain layer";
    public const string ApplicationLayer = "AD-2 application layer";
    public const string ApiLayer = "AD-2 api layer";
    public const string NoPublicSetters = "AD-4 no public setters";

    private const string DomainNamespace = @"^SlotBook\..*\.Domain(\..*)?$";
    private const string ApplicationNamespace = @"^SlotBook\..*\.Application(\..*)?$";
    private const string InfrastructureNamespace = @"^SlotBook\..*\.Infrastructure(\..*)?$";
    private const string ApiNamespace = @"^SlotBook\.Api(\..*)?$";
    private const string MongoNamespace = @"^MongoDB(\..*)?$";
    private const string MassTransitNamespace = @"^MassTransit(\..*)?$";

    public static IArchRule DomainLayerRule { get; } =
        Types().That().ResideInNamespaceMatching(DomainNamespace)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(ApplicationNamespace)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(InfrastructureNamespace)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(MongoNamespace)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(MassTransitNamespace)
            .Because(DomainLayer);

    public static IArchRule ApplicationLayerRule { get; } =
        Types().That().ResideInNamespaceMatching(ApplicationNamespace)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(InfrastructureNamespace)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(MongoNamespace)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(MassTransitNamespace)
            .Because(ApplicationLayer);

    public static IArchRule ApiLayerRule { get; } =
        Types().That().ResideInNamespaceMatching(ApiNamespace)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(MongoNamespace)
            .AndShould().NotDependOnAnyTypesThat().ResideInNamespaceMatching(MassTransitNamespace)
            .Because(ApiLayer);

    public static IArchRule NoPublicSettersRule { get; } =
        PropertyMembers().That().AreDeclaredIn(Types().That().ResideInNamespaceMatching(DomainNamespace))
            .Should().FollowCustomCondition(
                property => property.Writability == Writability.ReadOnly || property.SetterVisibility == Visibility.Private,
                "have no setter that is visible outside the type",
                "has a setter that is visible outside the type")
            .Because(NoPublicSetters);

    public static IArchRule ByName(string ruleName) => ruleName switch
    {
        DomainLayer => DomainLayerRule,
        ApplicationLayer => ApplicationLayerRule,
        ApiLayer => ApiLayerRule,
        NoPublicSetters => NoPublicSettersRule,
        _ => throw new ArgumentOutOfRangeException(nameof(ruleName), ruleName, null),
    };

    /// <summary>Evaluates a rule; each failure reads "rule name: failure text".</summary>
    public static IReadOnlyList<string> Violations(this IArchRule rule, Architecture architecture) =>
        [.. rule.Evaluate(architecture).Where(result => !result.Passed).Select(result => $"{RuleName(rule)}: {result.Description}")];

    private static string RuleName(IArchRule rule) => rule.Description[(rule.Description.LastIndexOf(" because ", StringComparison.Ordinal) + " because ".Length)..];
}
