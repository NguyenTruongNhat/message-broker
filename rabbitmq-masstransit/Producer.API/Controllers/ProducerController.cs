using MassTransit;
using MassTransit.Transports;
using Microsoft.AspNetCore.Mvc;
using Model.Share;
using Model.Share.dto;
using System;
using System.Threading.Tasks;

namespace Producer.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProducerController : ControllerBase
    {
        private readonly ILogger<ProducerController> _logger;
        // event
        private readonly IPublishEndpoint _publish;

        // command
        private readonly ISendEndpointProvider _send;


        public ProducerController(ILogger<ProducerController> logger, IPublishEndpoint publishEndpoint, ISendEndpointProvider sendEndpointProvider)
        {
            _logger = logger;
            _publish = publishEndpoint;
            _send = sendEndpointProvider;
        }

        [HttpPost("event")]
        public async Task<IActionResult> PublishEvent(int type)
        {
            var routingKey = type switch
            {
                1 => "email",
                2 => "sms",
                _ => throw new ArgumentException("Invalid Type")
            };

            var message = new NotificationEvent
            {
                Id = Guid.NewGuid(),
                Message = "Event Name",
                Type = type // 1 = Email, 2 = SMS
            };

            await _publish.Publish(message, context =>
            {
                context.SetRoutingKey(routingKey); // 🧭 Gán routing key
            });

            return Ok($"Notification published with routing key '{routingKey}'");
        }


        [HttpPut("command")]
        public async Task<IActionResult> UpdateProduct( [FromBody] UpdateProductDto dto)
        {
            // Send command to specific queue
            var endpoint = await _send.GetSendEndpoint(new Uri("queue:product-update-queue"));

            var command = new UpdateProduct
            {
                ProductId = Guid.NewGuid(),
                Name = DateTime.Now.ToString(),
                Price = dto.Price
            };

            await endpoint.Send(command);

            return Ok(new { message = "UpdateProduct command sent", command.ProductId });
        }
    }
}
