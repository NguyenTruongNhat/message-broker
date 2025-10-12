using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using static Model.Share.Constant;

var factory = new ConnectionFactory { HostName = "localhost", Password = "pass", UserName = "sa", VirtualHost = "nhatnguyen" };

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(exchange: MyExchange.Direct, type: ExchangeType.Direct);

await channel.QueueDeclareAsync(MyQueue.DirectQueue1, durable: false, exclusive: false, autoDelete: false);

await channel.QueueBindAsync(queue: MyQueue.DirectQueue1, exchange: MyExchange.Direct, routingKey: MyRoutingKey.Direct);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" {ea.Exchange} --- {ea.RoutingKey} ----- [Message] ::: {message}");
    return Task.CompletedTask;
};

await channel.BasicConsumeAsync(queue: MyQueue.DirectQueue1, autoAck: true, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();