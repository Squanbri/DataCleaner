using System.Text;
using System.Text.Json;
using DataCleaner.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DataCleaner.Api.Messaging;

public sealed class ImportProcessor(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnection connection,
    ILogger<ImportProcessor> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Import consumer crashed; reconnecting in 3 seconds");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            await HandleMessageAsync(channel, args, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: connection.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Import consumer listening on queue {Queue}", connection.QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // graceful shutdown
        }
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        CancellationToken stoppingToken)
    {
        try
        {
            var json = Encoding.UTF8.GetString(args.Body.Span);
            var message = JsonSerializer.Deserialize<ImportQueueMessage>(json, JsonOptions);
            if (message is null || message.BatchId <= 0)
            {
                logger.LogWarning("Invalid import message payload: {Payload}", json);
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var imports = scope.ServiceProvider.GetRequiredService<ImportService>();
            await imports.ProcessAsync(message.BatchId, stoppingToken);

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process import message");
            try
            {
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, cancellationToken: CancellationToken.None);
            }
            catch (Exception nackEx)
            {
                logger.LogError(nackEx, "Failed to nack import message");
            }
        }
    }
}
