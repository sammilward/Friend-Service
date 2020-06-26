namespace FriendService.RabbitMQ.Requests
{
    public class GetAllFriendsRabbitRequest
    {
        public string Id { get; set; }
        public bool? Requests { get; set; }
        public bool? Requested { get; set; }
    }
}
