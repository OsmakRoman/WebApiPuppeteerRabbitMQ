using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using PuppeteerSharp;
using System.Text;


namespace HtmlToPdfWorkerRabbitMQ
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var nameBytes = (byte[])ea.BasicProperties.Headers["filename"];
                    string filenamehtml = Encoding.UTF8.GetString(nameBytes);
                    File.WriteAllBytes(filenamehtml, body);
                    Console.WriteLine(" File {0} received from -queue.toworker-.", filenamehtml);

                    await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                    var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                    var page = await browser.NewPageAsync();
                    string markup = File.ReadAllText(filenamehtml);

                    File.Delete(filenamehtml);

                    await page.SetContentAsync(markup);
                    string filenamepdf = Path.ChangeExtension(filenamehtml, ".pdf");
                    await page.PdfAsync(filenamepdf);
                    await browser.CloseAsync();

                    byte[] pdfbody = File.ReadAllBytes(filenamepdf);
                    IBasicProperties props = channel.CreateBasicProperties();
                    props.Headers = new Dictionary<string, object>();
                    props.Headers.Add("filename", filenamepdf);

                    channel.BasicPublish(exchange: "exchange",
                                         routingKey: "fromworker",
                                         basicProperties: props,
                                         body: pdfbody);
                    File.Delete(filenamepdf);
                    Console.WriteLine(" File {0} sent to -queue.fromworker-.", filenamepdf);
                };
                channel.BasicConsume(queue: "queue.toworker",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();               
             }
        }
    }
}