using System.Text.Json;
using RabbitMQ.Client;

namespace DataCleaner.Api.Messaging;

public sealed class ImportPublisher(RabbitMqConnection connection)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new ImportQueueMessage(batchId), JsonOptions);

        await using var channel = await connection.CreateChannelAsync(cancellationToken);
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: connection.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: payload,
            cancellationToken: cancellationToken);
    }
}

public sealed record ImportQueueMessage(int BatchId);
