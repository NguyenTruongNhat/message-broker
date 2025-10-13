using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Model.Share;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<UpdateProductConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host("localhost", "nhatnguyen", h =>
                {
                    h.Username("sa");
                    h.Password("pass");
                });

                cfg.ReceiveEndpoint("product-update-queue", e =>
                {
                    e.ConfigureConsumer<UpdateProductConsumer>(ctx);
                });
            });
        });
    })
    .Build();
Console.WriteLine(">>>>>>>>>>>   Command Consumer    <<<<<<<<<<<<");
await host.RunAsync();

public class UpdateProductConsumer : IConsumer<UpdateProduct>
{
    public async Task Consume(ConsumeContext<UpdateProduct> context)
    {
        var msg = context.Message;
        Console.WriteLine($"🛠️ Received UpdateProduct Command: {msg.ProductId} - {msg.Name} (${msg.Price})");

        // Simulate product update in DB
        await Task.Delay(500);

        // handle after update, publish event
        await context.Publish(new ProductUpdated2
        {
            ProductId = msg.ProductId,
            Name = msg.Name,
            Price = msg.Price,
            UpdatedAt = DateTime.UtcNow
        });

        Console.WriteLine($"✅ Published ProductUpdated Event: {msg.ProductId}");
    }
}
