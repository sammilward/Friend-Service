using System.Threading.Tasks;

namespace FriendService.RabbitMQ.Producer
{
    public interface IFriendServiceRabbitRPCService
    {
        Task<T> PublishRabbitMessageWaitForResponseAsync<T>(string method, object requestModel);
    }
}
