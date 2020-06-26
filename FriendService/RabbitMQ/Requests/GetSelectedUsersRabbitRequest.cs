using System.Collections.Generic;

namespace FriendService.RabbitMQ.Requests
{
    public class GetSelectedUsersRabbitRequest
    {
        public List<string> UserIds { get; set; }
    }
}
