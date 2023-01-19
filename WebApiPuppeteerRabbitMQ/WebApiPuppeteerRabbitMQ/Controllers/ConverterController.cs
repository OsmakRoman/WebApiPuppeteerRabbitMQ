using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;


namespace WebApiPuppeteerRabbitMQ.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConverterController : ControllerBase
    {

        private readonly ILogger _logger;
        public ConverterController(ILogger<ConverterController> logger)
        {
            _logger = logger;
        }

        [HttpGet("ready/{id}")]
        public IActionResult Ready(string id)
        {
            foreach (string f in Directory.GetFiles("Files"))
            {
                if (Path.GetFileName(f).Split(".")[0] == id)
                return Content("Yes");
            }
            return Content("No");
        }

        [HttpGet("{id}")]
        public IActionResult Download(string id)
        {
            foreach (string f in Directory.GetFiles("Files"))
            {
                if (Path.GetFileName(f).Split(".")[0] == id)
                {
                    var bytes = System.IO.File.ReadAllBytes(f);
                    System.IO.File.Delete(f);
                    return File(bytes, "application/pdf", Path.GetFileName(f).Remove(0, 37));
                }               
            }
            
            return Content(id);
        }

        [HttpPost]
        public async Task<IActionResult> Upload()
        {
            var file = HttpContext.Request.Form.Files[0];
            var filename = file.FileName;
           
            Guid id = Guid.NewGuid();

            var filenamewithid = id + "." + filename;

            if (Path.GetExtension(filename).ToLower() == ".html")
            {
                if (file != null && file.Length > 0)
                {
                    byte[] body;
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);
                        body = stream.ToArray();
                        
                    }

                    var factory = new ConnectionFactory() { HostName = "localhost" };
                    using (var connection = factory.CreateConnection())
                    using (var channel = connection.CreateModel())
                    {
                        IBasicProperties props = channel.CreateBasicProperties();
                        props.Headers = new Dictionary<string, object>();
                        props.Headers.Add("filename", filenamewithid);
                        channel.BasicPublish(exchange: "exchange",
                                             routingKey: "toworker",
                                             basicProperties: props,
                                             body: body);
                        _logger.LogInformation("File {0} sent to -queue.toworker-.", filenamewithid);

    }

                    return Content(id.ToString());
                }
                else
                {
                    return Content("File is empty");
                }
            }

            return Content("File is not html");
        }

    }
}