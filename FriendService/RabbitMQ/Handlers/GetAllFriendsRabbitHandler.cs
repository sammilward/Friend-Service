using FriendService.DataAccess;
using FriendService.RabbitMQ.Producer;
using FriendService.RabbitMQ.Requests;
using FriendService.RabbitMQ.Responses;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prometheus;
using RabbitMQHelper;
using RabbitMQHelper.Models;
using System.Threading.Tasks;

namespace FriendService.RabbitMQ.Handlers
{
    public class GetAllFriendRabbitHandler : RabbitMessageHandler
    {
        protected override string MethodCanHandle => "GetAllFriends";

        private const string GetSelectedUsersMethod = "GetSelectedUsers";

        private readonly ILogger<GetAllFriendRabbitHandler> _logger;
        private readonly IFriendServiceRabbitRPCService _friendServiceRabbitRPCService;
        private readonly IFriendRepository _friendRepository;

        private readonly Counter rabbitMessagesRecievedCounter = Metrics.CreateCounter("GetAllFriendsRabbitMessagesRecieved", "Number of rabbit messages recieved to GetAllFriends handler");
        private readonly Counter successfullyGetAllFriendsRequestCounter = Metrics.CreateCounter("successfullyGetAllFriends", "Number of successfully GetAll friends requests");

        public GetAllFriendRabbitHandler(ILogger<GetAllFriendRabbitHandler> logger, IFriendServiceRabbitRPCService friendServiceRabbitRPCService, IFriendRepository friendRepository)
        {
            _logger = logger;
            _friendServiceRabbitRPCService = friendServiceRabbitRPCService;
            _friendRepository = friendRepository;
        }

        protected override async Task<object> ConvertMessageAndHandle(RabbitMessageRequestModel messageRequest)
        {
            rabbitMessagesRecievedCounter.Inc();
            _logger.LogInformation($"{nameof(GetAllFriendRabbitHandler)}.{nameof(ConvertMessageAndHandle)}: Converting message.");

            return await HandleMessageAsync(JsonConvert.DeserializeObject<GetAllFriendsRabbitRequest>(messageRequest.Data.ToString()));
        }

        private async Task<object> HandleMessageAsync(GetAllFriendsRabbitRequest getAllFriendsRabbitRequest)
        {
            var userIds = _friendRepository.GetAllIds(getAllFriendsRabbitRequest);

            var getSelectedUsersRabbitRequest = new GetSelectedUsersRabbitRequest()
            {
                UserIds = userIds
            };

            _logger.LogInformation($"{nameof(GetAllFriendRabbitHandler)}.{nameof(HandleMessageAsync)}: Sending request to UserService for method {GetSelectedUsersMethod}.");
            var getSelectedUsersRabbitResponse = await _friendServiceRabbitRPCService.PublishRabbitMessageWaitForResponseAsync<GetSelectedUsersRabbitResponse>(GetSelectedUsersMethod, getSelectedUsersRabbitRequest);

            var getAllFriendsRabbitResponse = new GetAllFriendsRabbitResponse();

            getAllFriendsRabbitResponse.FoundUsers = getSelectedUsersRabbitResponse.FoundUsers;

            if (getSelectedUsersRabbitResponse.FoundUsers)
            {
                getAllFriendsRabbitResponse.Users = getSelectedUsersRabbitResponse.Users;
            }
            successfullyGetAllFriendsRequestCounter.Inc();
            return getAllFriendsRabbitResponse;
        }
    }
}
