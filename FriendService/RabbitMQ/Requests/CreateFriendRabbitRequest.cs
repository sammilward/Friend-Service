namespace FriendService.RabbitMQ.Requests
{
    public class CreateFriendRabbitRequest
    {
        public string SenderId { get; set; }
        public string RecieverId { get; set; }
    }
}
