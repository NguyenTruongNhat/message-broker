using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.Mvc;
using Model.Share;
using System.Text.Json;

namespace Producer.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KafkaController : ControllerBase
    {
        private readonly string _topic = "product-update-topic";
        private readonly string _event_topic = "product-update-topic";
        private readonly ProducerConfig _config;

        public KafkaController()
        {
            _config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };
        }

        public record ProductUpdated(Guid ProductId, string Name, decimal Price);

        [HttpPost("publish-command")]
        public async Task<IActionResult> Publish([FromBody] ProductUpdated request)
        {
            using var producer = new ProducerBuilder<Null, string>(_config).Build();
            var message = new Message<Null, string>
            {
                Value = JsonSerializer.Serialize(request)
            };

            var deliveryResult = await producer.ProduceAsync(_topic, message);

            return Ok(new
            {
                Message = "✅ Message published to Kafka!",
                Topic = deliveryResult.Topic,
                Offset = deliveryResult.Offset,
                Partition = deliveryResult.Partition
            });
        }

        [HttpPost("publish-event")]
        public async Task<IActionResult> SendNotification([FromBody] NotificationMessage msg)
        {
            string topic = msg.Type switch
            {
                1 => "email-topic",
                2 => "sms-topic",
                _ => throw new ArgumentException("Invalid type")
            };

            using var producer = new ProducerBuilder<Null, string>(_config).Build();
            string json = JsonSerializer.Serialize(msg);

            await producer.ProduceAsync(topic, new Message<Null, string> { Value = json });

            return Ok(new { status = "sent", topic });
        }

        [HttpPost("create-topic")]
        public async Task<OkResult> CreateTopic()
        {
            var adminConfig = new AdminClientConfig { BootstrapServers = "localhost:9092" };
            using var adminClient = new AdminClientBuilder(adminConfig).Build();

            try
            {
                await adminClient.CreateTopicsAsync(new TopicSpecification[]
                {
                    new TopicSpecification { Name = "email-topic", NumPartitions = 1, ReplicationFactor = 1 },
                    new TopicSpecification { Name = "sms-topic", NumPartitions = 1, ReplicationFactor = 1 }
                });
            }
            catch (CreateTopicsException e)
            {
                Console.WriteLine($"⚠️ Topic creation: {e.Results[0].Error.Reason}");
            }

            return Ok();
        }


    }


}

