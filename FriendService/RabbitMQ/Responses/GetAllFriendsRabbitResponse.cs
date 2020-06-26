using FriendService.Models;
using System.Collections.Generic;

namespace FriendService.RabbitMQ.Responses
{
    public class GetAllFriendsRabbitResponse
    {
        public bool FoundUsers { get; set; }
        public List<User> Users { get; set; }
    }
}
