namespace FriendService.RabbitMQ.Requests
{
    public class ResponseToFriendRabbitRequest
    {
        public string RecieverId { get; set; }
        public string SenderId { get; set; }
        public bool? Accept { get; set; }
        public bool? Reject { get; set; }
    }
}
