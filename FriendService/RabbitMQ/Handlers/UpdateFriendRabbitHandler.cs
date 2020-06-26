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
    public class UpdateFriendRabbitHandler : RabbitMessageHandler
    {
        protected override string MethodCanHandle => "UpdateFriend";

        private const string UserExistsMethod = "UserExists";

        private readonly ILogger<CreateFriendRabbitHandler> _logger;
        private readonly IFriendServiceRabbitRPCService _friendServiceRabbitRPCService;
        private readonly IFriendRepository _friendRepository;

        private readonly Counter rabbitMessagesRecievedCounter = Metrics.CreateCounter("UpdateFriendRabbitMessagesRecieved", "Number of rabbit messages recieved to create friend handler");
        private readonly Counter successfullyUpdatedFriendRequestCounter = Metrics.CreateCounter("successfullyUpdatedFriendRequests", "Number of successfully updated friend requests");
        private readonly Counter unsucccessfulUpdatedFriendRequestCounter = Metrics.CreateCounter("unsucccessfulUpdatedFriendRequest", "Number of unsuccessfull updated friend requests");

        public UpdateFriendRabbitHandler(ILogger<CreateFriendRabbitHandler> logger, IFriendServiceRabbitRPCService friendServiceRabbitRPCService, IFriendRepository friendRepository)
        {
            _logger = logger;
            _friendServiceRabbitRPCService = friendServiceRabbitRPCService;
            _friendRepository = friendRepository;
        }

        protected override async Task<object> ConvertMessageAndHandle(RabbitMessageRequestModel messageRequest)
        {
            rabbitMessagesRecievedCounter.Inc();
            _logger.LogInformation($"{nameof(UpdateFriendRabbitHandler)}.{nameof(ConvertMessageAndHandle)}: Converting message.");

            return await HandleMessageAsync(JsonConvert.DeserializeObject<ResponseToFriendRabbitRequest>(messageRequest.Data.ToString()));
        }

        private async Task<object> HandleMessageAsync(ResponseToFriendRabbitRequest responseToFriendRabbitRequest)
        {
            _logger.LogInformation($"{nameof(UpdateFriendRabbitHandler)}.{nameof(HandleMessageAsync)}: Sending request to UserService for method {UserExistsMethod}.");
            var userExistsRabbitResponse = await _friendServiceRabbitRPCService.PublishRabbitMessageWaitForResponseAsync<UserExistsRabbitResponse>(UserExistsMethod, new UserExistsRabbitRequest() { Id = responseToFriendRabbitRequest.SenderId });

            var createFriendRabbitResponse = new CreateFriendRabbitResponse();

            if (userExistsRabbitResponse.Exists)
            {
                try
                {
                    await _friendRepository.UpdateAsync(responseToFriendRabbitRequest);
                    createFriendRabbitResponse.Successful = true;
                    successfullyUpdatedFriendRequestCounter.Inc();
                }
                catch (InvalidOperationException e)
                {
                    _logger.LogInformation($"{nameof(UpdateFriendRabbitHandler)}.{nameof(HandleMessageAsync)}: Can't respond to a request that does not exist.");
                    unsucccessfulUpdatedFriendRequestCounter.Inc();
                }
            }
            else
            {
                _logger.LogInformation($"{nameof(UpdateFriendRabbitHandler)}.{nameof(HandleMessageAsync)}: Can't respond to a request with a user that does not exist {responseToFriendRabbitRequest.SenderId}");
                unsucccessfulUpdatedFriendRequestCounter.Inc();
            }

            return createFriendRabbitResponse;
        }
    }
}
