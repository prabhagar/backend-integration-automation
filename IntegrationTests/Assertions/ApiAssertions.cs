using FluentAssertions;
using IntegrationTests.Contracts;
using Microsoft.Playwright;

namespace IntegrationTests.Assertions;

public static class ApiAssertions
{
    public static void AssertOrderContract(OrderResponse order)
    {
        order.OrderId.Should().NotBeNullOrWhiteSpace();
        order.Status.Should().BeOneOf("Processed", "Pending", "Failed");
        order.TotalAmount.Should().BePositive();
        order.Currency.Should().HaveLength(3);
        order.LastUpdatedUtc.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(-1));
    }

    public static async Task AssertAcceptedAsync(IAPIResponse response)
    {
        response.Status.Should().Be(202, await response.TextAsync());
    }
}