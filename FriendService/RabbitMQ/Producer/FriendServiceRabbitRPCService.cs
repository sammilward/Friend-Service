using Microsoft.Extensions.Configuration;
using RabbitMQHelper.Interfaces;
using System.Threading.Tasks;

namespace FriendService.RabbitMQ.Producer
{
    public class FriendServiceRabbitRPCService : IFriendServiceRabbitRPCService
    {
        private readonly IRabbitMessageProducer _rabbitMessageProducer;
        private readonly IConfiguration _configuration;
        private readonly string _exchange;
        private readonly string _routingKey;

        public FriendServiceRabbitRPCService(IConfiguration configuration, IRabbitMessageProducer rabbitMessageProducer)
        {
            _rabbitMessageProducer = rabbitMessageProducer;
            _configuration = configuration;

            _exchange = _configuration.GetSection("RabbitMqUserExchange").Value;
            _routingKey = _configuration.GetSection("RabbitMqUserRoutingKey").Value;
        }

        public async Task<T> PublishRabbitMessageWaitForResponseAsync<T>(string method, object requestModel)
        {
            return await _rabbitMessageProducer.PublishAndGetResponseAsync<T>(_exchange, _routingKey, method, requestModel);
        }
    }
}
