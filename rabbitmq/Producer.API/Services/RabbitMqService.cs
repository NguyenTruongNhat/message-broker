using Microsoft.AspNetCore.Connections;
using RabbitMQ.Client;
using System.Text;
using static Model.Share.Constant;

namespace Producer.API.Services
{
    public class RabbitMqService
    {
        private readonly ConnectionFactory _factory;

        public RabbitMqService()
        {
            _factory = new ConnectionFactory
            {
                HostName = "localhost",
                Password = "pass",
                UserName = "sa",
                VirtualHost = "nhatnguyen"
            };
        }

        private async Task<IChannel> CreateChannelAsync()
        {
            var connection = await _factory.CreateConnectionAsync();
            return await connection.CreateChannelAsync();
        }

        public async Task Direct(string input)
        {
            await using var channel = await CreateChannelAsync();

            //await channel.ExchangeDeclareAsync(exchange: MyExchange.Direct, type: ExchangeType.Direct);

            var body = Encoding.UTF8.GetBytes(input);
            await channel.BasicPublishAsync(exchange: MyExchange.Direct, routingKey: MyRoutingKey.Direct, body: body);
        }

        public async Task Fanout(string message)
        {
            await using var channel = await CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: MyExchange.Fanount, type: ExchangeType.Fanout);

            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: MyExchange.Fanount, routingKey: string.Empty, body: body);
        }

        public async Task Topic(string input)
        {
            await using var channel = await CreateChannelAsync();

            //await channel.ExchangeDeclareAsync(exchange: MyExchange.Topic, type: ExchangeType.Topic);

            var body = Encoding.UTF8.GetBytes(input);
            await channel.BasicPublishAsync(exchange: MyExchange.Topic, routingKey: "system.important.log", body: body);
        }

        public async Task Header(string input)
        {
            await using var channel = await CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: MyExchange.Header, type: ExchangeType.Headers);
            var body = Encoding.UTF8.GetBytes(input);

            var props = new BasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = (DeliveryModes)2;
            props.Headers = new Dictionary<string, object>();
            props.Headers.Add("type", "log");
            props.Headers.Add("severity", "high");

            //props.Headers.Add("type", "audit");
            props.Headers.Add("department", "IT");

            // Gửi message với headers
            await channel.BasicPublishAsync(MyExchange.Header, "", true, props, body);
        }
    }
}
