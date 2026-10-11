namespace SlotBook.ArchitectureTests;

/// <summary>One bound input of an endpoint: a route, query or header value.</summary>
internal sealed record EndpointInput(string Endpoint, string Source, string Name);
