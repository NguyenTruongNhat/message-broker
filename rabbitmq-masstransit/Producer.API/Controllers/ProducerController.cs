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
        public async Task<IActionResult> PublishProductCreated()
        {
            var message = new ProductCreated
            {
                ProductId = Guid.NewGuid(),
                Name = "iPhone 16 Pro",
                Price = 1299.99m,
                CreatedAt = DateTime.UtcNow
            };

            await _publish.Publish(message);

            return Ok(new { message = "ProductCreated event published", message.ProductId });
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
