using FriendService.DataAccess;
using FriendService.RabbitMQ.Producer;
using FriendService.RabbitMQ.Requests;
using FriendService.RabbitMQ.Responses;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prometheus;
using RabbitMQHelper;
using RabbitMQHelper.Models;
using System;
using System.Threading.Tasks;

namespace FriendService.RabbitMQ.Handlers
{
    public class DeleteFriendRabbitHandler : RabbitMessageHandler
    {
        protected override string MethodCanHandle => "DeleteFriend";

        private const string UserExistsMethod = "UserExists";

        private readonly ILogger<DeleteFriendRabbitHandler> _logger;
        private readonly IFriendServiceRabbitRPCService _friendServiceRabbitRPCService;
        private readonly IFriendRepository _friendRepository;

        private readonly Counter rabbitMessagesRecievedCounter = Metrics.CreateCounter("DeleteFriendRabbitMessagesRecieved", "Number of rabbit messages recieved to delete friend handler");
        private readonly Counter successfullyDeletedFriendRequestCounter = Metrics.CreateCounter("successfullyDeletedFriend", "Number of successfully deleted friend requests");
        private readonly Counter unsucccessfulDeletedFriendRequestCounter = Metrics.CreateCounter("unsucccessfulDeletedFriendRequest", "Number of unsuccessfully deleted friends");

        public DeleteFriendRabbitHandler(ILogger<DeleteFriendRabbitHandler> logger, IFriendServiceRabbitRPCService friendServiceRabbitRPCService, IFriendRepository friendRepository)
        {
            _logger = logger;
            _friendServiceRabbitRPCService = friendServiceRabbitRPCService;
            _friendRepository = friendRepository;
        }

        protected override async Task<object> ConvertMessageAndHandle(RabbitMessageRequestModel messageRequest)
        {
            rabbitMessagesRecievedCounter.Inc();
            _logger.LogInformation($"{nameof(UpdateFriendRabbitHandler)}.{nameof(ConvertMessageAndHandle)}: Converting message.");

            return await HandleMessageAsync(JsonConvert.DeserializeObject<DeleteFriendRabbitRequest>(messageRequest.Data.ToString()));
        }

        private async Task<object> HandleMessageAsync(DeleteFriendRabbitRequest deleteFriendRabbitRequest)
        {
            _logger.LogInformation($"{nameof(UpdateFriendRabbitHandler)}.{nameof(HandleMessageAsync)}: Sending request to UserService for method {UserExistsMethod}.");
            var userExistsRabbitResponse = await _friendServiceRabbitRPCService.PublishRabbitMessageWaitForResponseAsync<UserExistsRabbitResponse>(UserExistsMethod, new UserExistsRabbitRequest() { Id = deleteFriendRabbitRequest.RecieverId });

            var deleteFriendRabbitResponse = new DeleteFriendRabbitResponse();

            if (userExistsRabbitResponse.Exists)
            {
                await _friendRepository.UpdateAsync(deleteFriendRabbitRequest);
                deleteFriendRabbitResponse.Successful = true;
                successfullyDeletedFriendRequestCounter.Inc();
            }
            else
            {
                _logger.LogInformation($"{nameof(UpdateFriendRabbitHandler)}.{nameof(HandleMessageAsync)}: Can't delete a friend relationship with a user that does not exist {deleteFriendRabbitRequest.RecieverId}");
                unsucccessfulDeletedFriendRequestCounter.Inc();
            }

            return deleteFriendRabbitResponse;
        }
    }
}
