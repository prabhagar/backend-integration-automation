namespace IntegrationTests.Contracts;

public sealed record SalesforceLeadEvent(
    string LeadId,
    string FirstName,
    string LastName,
    string Company,
    string Email,
    string CorrelationId,
    DateTimeOffset CreatedAtUtc);

public sealed record SqsLeadEnvelope(
    string MessageId,
    SalesforceLeadEvent Lead,
    string SourceQueue,
    DateTimeOffset EnqueuedAtUtc);

public sealed record ProfileCreateRequest(
    string LeadId,
    string FullName,
    string Email,
    string CorrelationId);

public sealed record ClientCreateRequest(
    string LeadId,
    string AccountName,
    string Email,
    string CorrelationId);

public sealed record TicketCreateRequest(
    string LeadId,
    string Subject,
    string Description,
    string CorrelationId);

public sealed record LeadProcessingResult(
    string LeadId,
    string ProfileId,
    string ClientId,
    string TicketId);
