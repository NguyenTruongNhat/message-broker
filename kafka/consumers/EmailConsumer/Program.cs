using Confluent.Kafka;
using System.Text.Json;

var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "email-consumer-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
consumer.Subscribe("email-topic");

Console.WriteLine("📧 EmailConsumer listening...");

try
{
    while (true)
    {
        var cr = consumer.Consume();
        var msg = JsonSerializer.Deserialize<NotificationMessage>(cr.Message.Value);
        Console.WriteLine($"📧 Email received: {msg?.Message}");
    }
}
catch (OperationCanceledException) { }
finally
{
    consumer.Close();
}

record NotificationMessage(int Type, string Message);



