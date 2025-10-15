using MassTransit;
using MassTransit.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model.Share;
using RabbitMQ.Client;
using static MassTransit.Logging.OperationName;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<Sms1Consumer>();
            x.AddConsumer<Email1Consumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host("localhost", "nhatnguyen", h =>
                {
                    h.Username("sa");
                    h.Password("pass");
                });

                // Exchange chính (direct)
                cfg.Message<NotificationEvent>(x =>
                {
                    x.SetEntityName("notification-exchange"); // tên exchange
                });

                ///
                /// When a consumer starts up, MassTransit will:
                /// Check if an exchange for the NotificationEvent message already exists.
                /// If it doesn't exist, it will automatically create the exchange (by default, a fanout exchange).
                /// Then, it will bind the consumer's queue to that exchange."
                ///
                cfg.Publish<NotificationEvent>(x =>
                {
                    x.ExchangeType = ExchangeType.Direct;
                });

                // 🎯 Queue 1: EmailConsumer
                cfg.ReceiveEndpoint("email-queue", e =>
                {
                    e.Bind("notification-exchange", x =>
                    {
                        x.RoutingKey = "email";
                        x.ExchangeType = ExchangeType.Direct;
                    });
                    e.Consumer<Email1Consumer>();
                });

                // 🎯 Queue 2: SmsConsumer
                cfg.ReceiveEndpoint("sms-queue", e =>
                {
                    e.Bind("notification-exchange", x =>
                    {
                        x.RoutingKey = "sms";
                        x.ExchangeType = ExchangeType.Direct;
                    });
                    e.Consumer<Sms1Consumer>();
                });

            });
        });
    })
    .Build();

Console.WriteLine(">>>>>>>>>>>   Event Consumer <<<<<<<<<<<<");
await host.RunAsync();

public class Email1Consumer : IConsumer<NotificationEvent>
{
    public async Task Consume(ConsumeContext<NotificationEvent> context)
    {
        Console.WriteLine($"EmailConsumer ++++ {context.Message.Message}");
        await Task.CompletedTask;
    }
}

public class Sms1Consumer : IConsumer<NotificationEvent>
{
    public async Task Consume(ConsumeContext<NotificationEvent> context)
    {
        Console.WriteLine($"SmsConsumer ===: {context.Message.Message}");
        await Task.CompletedTask;
    }
}

