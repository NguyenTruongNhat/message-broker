using Confluent.Kafka;
using System;
using System.Threading;

class Program
{
    static async Task Main()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "product-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe("product-update-topic");

        Console.WriteLine("🚀 Listening for messages... (Press Ctrl+C to exit)");

        CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(cts.Token);
                    Console.WriteLine($"📨 Received message: {result.Message.Value}");
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"⚠️ Error: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}
