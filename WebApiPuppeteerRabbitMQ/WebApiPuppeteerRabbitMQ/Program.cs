using Microsoft.AspNetCore.Http.Features;
using WebApiPuppeteerRabbitMQ.Services;

namespace WebApiPuppeteerRabbitMQ
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddHostedService<PdfConsumerHostedService>();
            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: "AllowOrigin",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    });
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseAuthorization();


            app.MapControllers();
            app.UseCors("AllowOrigin");
            app.UseStaticFiles();


            app.Run();
        }
    }
}