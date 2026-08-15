using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NotificationService.Application.Dtos;
using NotificationService.Infrastructure.Messaging.Schemas;
using NotificationService.IntegrationTests.Fixtures;

namespace NotificationService.IntegrationTests;

/// <summary>
/// Proves the cross-service, event-driven flow end to end: real Avro messages are published onto
/// auth.user-registered.v1 and order.paid.v1 (the same producer/schema shapes AuthenticationService
/// and OrderService use), and this service's own consumers — already running inside the
/// WebApplicationFactory host — are left to pick them up on their own, with no direct call into
/// the application under test. Mirrors ShippingService's OrderPaidConsumerTests.
/// </summary>
[Collection("NotificationApi")]
public class NotificationFlowTests
{
    private readonly NotificationApiFixture _fixture;

    public NotificationFlowTests(NotificationApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/notifications/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserRegisteredThenOrderPaid_SendsNotificationToTheRegisteredEmail()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        const string email = "flow-test@example.com";

        await PublishUserRegisteredAsync(userId, email);
        // Give the contact-recording consumer a moment to land before the order.paid.v1 message
        // arrives — otherwise this is a legitimate race this platform accepts (see README: the
        // handler records a Failed/no-known-email notification rather than erroring, and a later
        // notification for the same order would still succeed).
        await Task.Delay(TimeSpan.FromSeconds(2));
        await PublishOrderPaidAsync(orderId, userId);

        var client = AuthedClient(userId);
        var notification = await PollForNotificationAsync(client, orderId.ToString(), TimeSpan.FromSeconds(30));

        Assert.Equal("Sent", notification.Status);
        Assert.Equal("Email", notification.Channel);
        Assert.Equal(email, notification.Recipient);
        Assert.Equal("OrderPaid", notification.Type);
        Assert.Contains(_fixture.EmailGateway.Sent, s => s.To == email);
    }

    [Fact]
    public async Task UserRegisteredWithPhoneThenOrderPaid_SendsBothEmailAndSmsNotifications()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        const string email = "flow-test-sms@example.com";
        const string phoneNumber = "+15559876543";

        await PublishUserRegisteredAsync(userId, email, phoneNumber);
        await Task.Delay(TimeSpan.FromSeconds(2));
        await PublishOrderPaidAsync(orderId, userId);

        var client = AuthedClient(userId);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        List<NotificationDto> matches = [];
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync("/api/notifications/me?page=1&pageSize=20");
            var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>() ?? [];
            matches = notifications.Where(n => n.Subject.Contains(orderId.ToString()) || n.Body.Contains(orderId.ToString())).ToList();
            if (matches.Count >= 2)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.Contains(matches, n => n.Channel == "Email" && n.Recipient == email && n.Status == "Sent");
        Assert.Contains(matches, n => n.Channel == "Sms" && n.Recipient == phoneNumber && n.Status == "Sent");
        Assert.Contains(_fixture.EmailGateway.Sent, s => s.To == email);
        Assert.Contains(_fixture.SmsGateway.Sent, s => s.To == phoneNumber);
    }

    [Fact]
    public async Task PaymentFailed_SendsNotificationIncludingFailureReason()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        const string email = "decline-test@example.com";

        await PublishUserRegisteredAsync(userId, email);
        await Task.Delay(TimeSpan.FromSeconds(2));
        await PublishPaymentFailedAsync(orderId, userId, "Your card was declined.");

        var client = AuthedClient(userId);
        var notification = await PollForNotificationAsync(client, orderId.ToString(), TimeSpan.FromSeconds(30));

        Assert.Equal("PaymentFailed", notification.Type);
        Assert.Contains("Your card was declined.", notification.Body);
    }

    private async Task PublishUserRegisteredAsync(Guid userId, string email, string? phoneNumber = null)
    {
        using var schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = _fixture.SchemaRegistryUrl });
        var producerConfig = new ProducerConfig { BootstrapServers = _fixture.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, UserRegisteredAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<UserRegisteredAvro>(schemaRegistryClient))
            .Build();

        await producer.ProduceAsync("auth.user-registered.v1", new Message<string, UserRegisteredAvro>
        {
            Key = userId.ToString(),
            Value = new UserRegisteredAvro { UserId = userId.ToString(), Email = email, PhoneNumber = phoneNumber, RegisteredAt = DateTime.UtcNow }
        });
        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private async Task PublishOrderPaidAsync(Guid orderId, Guid userId)
    {
        using var schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = _fixture.SchemaRegistryUrl });
        var producerConfig = new ProducerConfig { BootstrapServers = _fixture.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, OrderPaidAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderPaidAvro>(schemaRegistryClient))
            .Build();

        await producer.ProduceAsync("order.paid.v1", new Message<string, OrderPaidAvro>
        {
            Key = orderId.ToString(),
            Value = new OrderPaidAvro { OrderId = orderId.ToString(), UserId = userId.ToString(), PaidAt = DateTime.UtcNow, ShippingAddress = "1 Main St" }
        });
        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private async Task PublishPaymentFailedAsync(Guid orderId, Guid userId, string failureReason)
    {
        using var schemaRegistryClient = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = _fixture.SchemaRegistryUrl });
        var producerConfig = new ProducerConfig { BootstrapServers = _fixture.KafkaBootstrapServers };

        using var producer = new ProducerBuilder<string, PaymentFailedAvro>(producerConfig)
            .SetValueSerializer(new AvroSerializer<PaymentFailedAvro>(schemaRegistryClient))
            .Build();

        await producer.ProduceAsync("payment.failed.v1", new Message<string, PaymentFailedAvro>
        {
            Key = orderId.ToString(),
            Value = new PaymentFailedAvro
            {
                PaymentId = Guid.NewGuid().ToString(),
                OrderId = orderId.ToString(),
                UserId = userId.ToString(),
                FailureReason = failureReason,
                FailedAt = DateTime.UtcNow
            }
        });
        producer.Flush(TimeSpan.FromSeconds(5));
    }

    private static async Task<NotificationDto> PollForNotificationAsync(HttpClient client, string orderIdFragment, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync("/api/notifications/me?page=1&pageSize=20");
            var notifications = await response.Content.ReadFromJsonAsync<List<NotificationDto>>() ?? [];
            var match = notifications.FirstOrDefault(n => n.Subject.Contains(orderIdFragment));
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"No notification mentioning order {orderIdFragment} appeared within {timeout}.");
    }

    private HttpClient AuthedClient(Guid userId)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokenFactory.CreateAccessToken(userId));
        return client;
    }
}
