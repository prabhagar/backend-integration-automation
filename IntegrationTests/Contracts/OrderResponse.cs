namespace IntegrationTests.Contracts;

public sealed record OrderResponse(
    string OrderId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset LastUpdatedUtc);