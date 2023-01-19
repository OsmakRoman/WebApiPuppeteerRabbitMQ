using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace WebApiPuppeteerRabbitMQ.Services
{
    public class PdfConsumerHostedService : IHostedService

    {
        private IModel? channel = null;
        private IConnection? connection = null;
        private readonly ILogger _logger;

        public PdfConsumerHostedService(ILogger<PdfConsumerHostedService> logger)
        {
            _logger = logger;
        }

        private void Run()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.ExchangeDeclare("exchange", "direct", true, false, null);
            
            channel.QueueDeclare(queue: "queue.toworker",
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);
            channel.QueueBind("queue.toworker", "exchange", "toworker");
           
            channel.QueueDeclare(queue: "queue.fromworker",
                      durable: true,
                      exclusive: false,
                      autoDelete: false,
                      arguments: null);
            channel.QueueBind("queue.fromworker", "exchange", "fromworker");
            
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += OnMessageRecieved;
            channel.BasicConsume(queue: "queue.fromworker",
                                 autoAck: true,
                                 consumer: consumer);
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.Run();
            return Task.CompletedTask;

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            channel?.Dispose();
            connection?.Dispose();
            return Task.CompletedTask;
        }

        private void OnMessageRecieved(object? model, BasicDeliverEventArgs args)
        {
            var body = args.Body.ToArray();
            var nameBytes = (byte[])args.BasicProperties.Headers["filename"];
            string filename = Encoding.UTF8.GetString(nameBytes);
            File.WriteAllBytes( Path.Combine("Files", filename), body);
            _logger.LogInformation("File {0} received from -queue.fromworker-.", filename);
        }
    }
}
