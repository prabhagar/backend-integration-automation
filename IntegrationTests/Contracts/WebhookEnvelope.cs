namespace IntegrationTests.Contracts;

public sealed record WebhookEnvelope(
    string EventType,
    string CorrelationId,
    string SourceSystem,
    Dictionary<string, object?> Payload);