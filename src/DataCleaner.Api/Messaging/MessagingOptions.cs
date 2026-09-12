namespace DataCleaner.Api.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";
    public string QueueName { get; set; } = "imports";
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string UploadsPath { get; set; } = "./uploads";
}
