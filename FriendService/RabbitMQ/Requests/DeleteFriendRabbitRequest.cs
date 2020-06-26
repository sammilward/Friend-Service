namespace FriendService.RabbitMQ.Requests
{
    public class DeleteFriendRabbitRequest
    {
        public string SenderId { get; set; }
        public string RecieverId { get; set; }
    }
}
