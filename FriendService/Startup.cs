using FriendService.DataAccess;
using FriendService.RabbitMQ.Producer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace FriendService
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IFriendServiceRabbitRPCService, FriendServiceRabbitRPCService>();
            services.AddSingleton<IFriendRepository, MongoFriendRepository>();

            RabbitMQHelper.RabbitServiceRegistration.RegisterConsumorService(services);
            RabbitMQHelper.RabbitServiceRegistration.RegisterProducerService(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMetricServer();

            app.UseRouting();
        }
    }
}
