using FriendService.Models;

namespace FriendService.RabbitMQ.Responses
{
    public class GetFriendStatusRabbitResponse
    {
        public FriendStatusEnum FriendStatus { get; set; }
        public bool Successful { get; set; } = false;
    }
}
