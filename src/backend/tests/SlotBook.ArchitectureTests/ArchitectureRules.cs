using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
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
    public const string NoUnfilteredCollection = "AD-5 no unfiltered collection";
    public const string AggregateTenancy = "AD-5 aggregate tenancy";
    public const string IntegrationEvents = "AD-10 integration events";
    public const string ConsumerDefinitions = "AD-10 consumer definitions";

    private const string DomainNamespace = @"^SlotBook\..*\.Domain(\..*)?$";
    private const string ApplicationNamespace = @"^SlotBook\..*\.Application(\..*)?$";
    private const string InfrastructureNamespace = @"^SlotBook\..*\.Infrastructure(\..*)?$";
    private const string ApiNamespace = @"^SlotBook\.Api(\..*)?$";
    private const string MongoNamespace = @"^MongoDB(\..*)?$";
    private const string PersistenceNamespace = @"^SlotBook\.SharedKernel\.Infrastructure\.Persistence(\..*)?$";
    private const string AggregateRootName = "SlotBook.SharedKernel.Domain.AggregateRoot";
    private const string TenantAggregateRootName = "SlotBook.SharedKernel.Domain.TenantAggregateRoot";
    private const string GlobalAggregateAttributeName = "SlotBook.SharedKernel.Domain.GlobalAggregateAttribute";
    private const string MassTransitNamespace = @"^MassTransit(\..*)?$";
    private const string TenantEventName = "SlotBook.SharedKernel.Contracts.TenantEvent";
    private const string ConsumerInterfaceName = "MassTransit.IConsumer";
    private const string ConsumerDefinitionName = "SlotBook.SharedKernel.Infrastructure.Messaging.SlotBookConsumerDefinition`1";

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

    /// <summary>
    /// Only the persistence types of <c>SlotBook.SharedKernel.Infrastructure</c> ask the database for a
    /// collection; they apply the tenant filter (AD-5).
    /// </summary>
    public static IArchRule NoUnfilteredCollectionRule { get; } =
        Types().That().DoNotResideInNamespaceMatching(PersistenceNamespace)
            .Should().FollowCustomCondition(
                type => !type.Dependencies.OfType<MethodCallDependency>().Any(call => IsGetCollection(call.TargetMember)),
                "not ask the database for a collection",
                "asks the database for a collection")
            .Because(NoUnfilteredCollection);

    /// <summary>
    /// Every concrete aggregate is tenant-owned or <c>[GlobalAggregate]</c>, not both (AD-5). Runs over
    /// all types, so it never selects nothing; the production guard test proves the base types are loaded.
    /// </summary>
    public static IArchRule AggregateTenancyRule { get; } =
        Types().Should().FollowCustomCondition(
                type => TenancyProblem(type) is null,
                "be tenant-owned or [GlobalAggregate] when it is an aggregate",
                "is an aggregate that is not exactly one of tenant-owned and [GlobalAggregate]")
            .Because(AggregateTenancy);

    /// <summary>Every concrete integration event is sealed and named for what happened, without an <c>Event</c> suffix (AD-10).</summary>
    public static IArchRule IntegrationEventsRule { get; } =
        Types().That().AreNot(typeof(object)).Should().FollowCustomCondition(
                type => EventProblem(type) is null,
                "be sealed and have no Event suffix when it is a TenantEvent",
                "is a TenantEvent that is not sealed or has an Event suffix")
            .Because(IntegrationEvents);

    /// <summary>Every consumer has a definition derived from <c>SlotBookConsumerDefinition</c>, which carries the endpoint name and the retry rules (AD-10).</summary>
    public static IArchRule ConsumerDefinitionsRule { get; } =
        Types().Should().FollowCustomCondition(
                type => !IsConsumer(type) || type.BackwardsDependencies.Any(dependency => dependency.Origin is Class origin && DerivesFrom(origin, ConsumerDefinitionName)),
                "have a definition derived from SlotBookConsumerDefinition when it is a consumer",
                "is a consumer without a definition derived from SlotBookConsumerDefinition")
            .Because(ConsumerDefinitions);

    public static IArchRule ByName(string ruleName) => ruleName switch
    {
        DomainLayer => DomainLayerRule,
        ApplicationLayer => ApplicationLayerRule,
        ApiLayer => ApiLayerRule,
        NoPublicSetters => NoPublicSettersRule,
        NoUnfilteredCollection => NoUnfilteredCollectionRule,
        AggregateTenancy => AggregateTenancyRule,
        IntegrationEvents => IntegrationEventsRule,
        ConsumerDefinitions => ConsumerDefinitionsRule,
        _ => throw new ArgumentOutOfRangeException(nameof(ruleName), ruleName, null),
    };

    /// <summary>Evaluates a rule; each failure reads "rule name: failure text".</summary>
    public static IReadOnlyList<string> Violations(this IArchRule rule, Architecture architecture) =>
        [.. rule.Evaluate(architecture).Where(result => !result.Passed).Select(result => $"{RuleName(rule)}: {result.Description}")];

    private static string RuleName(IArchRule rule) => rule.Description[(rule.Description.LastIndexOf(" because ", StringComparison.Ordinal) + " because ".Length)..];

    private static bool IsGetCollection(IMember member) =>
        member.Name.StartsWith("GetCollection", StringComparison.Ordinal)
        && (member.DeclaringType.FullName.StartsWith("MongoDB.Driver.IMongoDatabase", StringComparison.Ordinal)
            || member.DeclaringType.FullName.StartsWith("MongoDB.Driver.MongoDatabase", StringComparison.Ordinal));

    // An aggregate derives from AggregateRoot or TenantAggregateRoot: both are checked because a
    // base class from an assembly that is not loaded (the fixtures) has no further base class.
    private static string? TenancyProblem(IType type)
    {
        if (type is not Class { IsAbstract: false } concrete || !(DerivesFrom(concrete, AggregateRootName) || DerivesFrom(concrete, TenantAggregateRootName)))
        {
            return null;
        }

        var tenantOwned = DerivesFrom(concrete, TenantAggregateRootName);
        var global = concrete.Attributes.Any(attribute => attribute.FullName == GlobalAggregateAttributeName);
        return tenantOwned == global ? type.FullName : null;
    }

    private static string? EventProblem(IType type)
    {
        if (type is not Class concrete || concrete.IsAbstract == true || !DerivesFrom(concrete, TenantEventName))
        {
            return null;
        }

        return concrete.IsSealed != true || concrete.Name.EndsWith("Event", StringComparison.Ordinal) ? type.FullName : null;
    }

    private static bool IsConsumer(IType type) =>
        type is Class { IsAbstract: false } consumer
        && consumer.ImplementedInterfaces.Any(contract => contract.FullName.Equals(ConsumerInterfaceName, StringComparison.Ordinal) || contract.FullName.StartsWith(ConsumerInterfaceName + "`1", StringComparison.Ordinal));

    private static bool DerivesFrom(Class type, string baseName)
    {
        for (var current = type.BaseClass; current is not null; current = current.BaseClass)
        {
            if (current.FullName == baseName)
            {
                return true;
            }
        }

        return false;
    }
}
