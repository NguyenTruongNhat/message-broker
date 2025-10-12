using Microsoft.AspNetCore.Mvc;
using Producer.API.Services;

namespace Producer.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProducerController : Controller
    {
        private readonly RabbitMqService service;
        public ProducerController(RabbitMqService _service)
        {
            service = _service;
        }

        [Route("fanout")]
        [HttpPost]
        public async Task Fanout([FromBody] string input)
        {
            await service.Fanout(input);
        }

        [Route("topic")]
        [HttpPost]
        public async Task Topic([FromBody] string input)
        {
            await service.Topic(input);
        }

        [Route("direct")]
        [HttpPost]
        public async Task Direct([FromBody] string input)
        {
            await service.Direct(input);
        }

        [Route("header")]
        [HttpPost]
        public async Task Header([FromBody] string input)
        {
            await service.Header(input);
        }



    }
}
