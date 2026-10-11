using System.Collections;
using System.Reflection;
using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Messaging;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Contracts;
using Xunit;

namespace SlotBook.IntegrationTests.Messaging;

/// <summary>
/// AD-19: every integration event survives MassTransit serialization. The set of events is found by
/// reflection over the test assembly and every SlotBook assembly that is loaded, so a module's events
/// join the test as soon as the module is referenced.
/// </summary>
public sealed class EventSerializationTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>
{
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(20);

    [Fact]
    public void TheTestFindsTheKnownEventTypes()
    {
        var types = EventTypes();

        types.ShouldContain(typeof(NoteCreated));
        types.ShouldContain(typeof(NoteArchived));
    }

    [Fact]
    public async Task EveryTenantEventRoundTripsThroughTheOutbox()
    {
        var ct = TestContext.Current.CancellationToken;
        var types = EventTypes();
        await using var factory = new SlotBookApiFactory { TenantSlug = "clinic-a", MongoDatabase = mongo.DatabaseName };
        var sent = types.Select(Sample).ToList();
        var sentIds = sent.Cast<TenantEvent>().Select(evt => evt.EventId).ToHashSet();
        await using var recorder = await EventRecorder.StartAsync(factory.Services.GetRequiredService<IBus>(), factory.EndpointPrefix + "-recorder", types, evt => sentIds.Contains(evt.EventId), ct);

        // Events only, no aggregate: the unit of work still opens its transaction for the outbox.
        using (var scope = factory.Services.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var add = typeof(IUnitOfWork).GetMethod(nameof(IUnitOfWork.AddIntegrationEvent))!;
            foreach (var evt in sent)
            {
                add.MakeGenericMethod(evt.GetType()).Invoke(unitOfWork, [evt]);
            }

            await unitOfWork.CommitAsync(ct);
        }

        var received = await recorder.WaitForAsync(sent.Count, _wait, ct);
        await OutboxDrain.WaitAsync(mongo.Database, _wait, ct);
        foreach (var evt in sent)
        {
            var match = received.Single(item => item.Message.GetType() == evt.GetType());
            ToJson(match.Message).ShouldBe(ToJson(evt), $"{evt.GetType().Name} must arrive unchanged");
            match.MessageId.ShouldBe(((TenantEvent)evt).EventId);
        }
    }

    private static string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType());

    private static List<Type> EventTypes()
    {
        _ = typeof(TenantEvent).Assembly;
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly == typeof(EventSerializationTests).Assembly
                || (assembly.GetName().Name?.StartsWith("SlotBook.", StringComparison.Ordinal) ?? false))
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsClass: true } && typeof(TenantEvent).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>An instance with every property set to a distinctive value.</summary>
    private static object Sample(Type type)
    {
        var instance = Activator.CreateInstance(type, nonPublic: true)!;
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.SetMethod is not null))
        {
            property.SetValue(instance, SampleValue(property.PropertyType, property.Name));
        }

        return instance;
    }

    private static object? SampleValue(Type type, string name)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(Guid))
        {
            return SlotBook.SharedKernel.Domain.DeterministicId.From(SlotBook.SharedKernel.Domain.DeterministicId.SlotBookNamespace, "sample:" + name);
        }

        if (type == typeof(string))
        {
            return "sample " + name;
        }

        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.FromHours(1));
        }

        if (type == typeof(DateTime))
        {
            return new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
        {
            return Convert.ChangeType(42, type, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).Cast<object>().Last();
        }

        if (type.IsGenericType && type.GetGenericArguments() is [var element] && typeof(IEnumerable).IsAssignableFrom(type))
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
            list.Add(SampleValue(element, name));
            return list;
        }

        if (type.IsClass)
        {
            return Sample(type);
        }

        throw new NotSupportedException($"The round-trip test has no sample value for {type} ({name}); teach SampleValue about it.");
    }
}
