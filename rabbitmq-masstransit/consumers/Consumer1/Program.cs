using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model.Share;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ProductCreatedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host("localhost", "nhatnguyen", h =>
                {
                    h.Username("sa");
                    h.Password("pass");
                });

                cfg.ReceiveEndpoint("product-created-queue", e =>
                {
                    e.ConfigureConsumer<ProductCreatedConsumer>(ctx);
                });
            });
        });
    })
    .Build();

Console.WriteLine(">>>>>>>>>>>   Event Consumer <<<<<<<<<<<<");
await host.RunAsync();

public class ProductCreatedConsumer : IConsumer<ProductCreated>
{
    public Task Consume(ConsumeContext<ProductCreated> context)
    {
        var msg = context.Message;
        Console.WriteLine($"Received:::: {msg.Name} ({msg.ProductId})");
        return Task.CompletedTask;
    }
}

