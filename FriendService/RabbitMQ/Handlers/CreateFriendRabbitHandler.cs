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
    public class CreateFriendRabbitHandler : RabbitMessageHandler
    {
        protected override string MethodCanHandle => "CreateFriend";

        private const string UserExistsMethod = "UserExists";

        private readonly ILogger<CreateFriendRabbitHandler> _logger;
        private readonly IFriendServiceRabbitRPCService _friendServiceRabbitRPCService;
        private readonly IFriendRepository _friendRepository;

        private readonly Counter rabbitMessagesRecievedCounter = Metrics.CreateCounter("CreateFriendRabbitMessagesRecieved", "Number of rabbit messages recieved to create friend handler");
        private readonly Counter successfullyCreatedFriendRequestCounter = Metrics.CreateCounter("successfullyCreatedFriendRequest", "Number of successfully created friend requests");
        private readonly Counter unsucccessfulCreatedFriendRequestCounter = Metrics.CreateCounter("unsucccessfulCreatedFriendRequest", "Number of unsuccessfull created friend requests");

        public CreateFriendRabbitHandler(ILogger<CreateFriendRabbitHandler> logger, IFriendServiceRabbitRPCService friendServiceRabbitRPCService, IFriendRepository friendRepository)
        {
            _logger = logger;
            _friendServiceRabbitRPCService = friendServiceRabbitRPCService;
            _friendRepository = friendRepository;
        }

        protected override async Task<object> ConvertMessageAndHandle(RabbitMessageRequestModel messageRequest)
        {
            rabbitMessagesRecievedCounter.Inc();
            _logger.LogInformation($"{nameof(CreateFriendRabbitHandler)}.{nameof(ConvertMessageAndHandle)}: Converting message.");

            return await HandleMessageAsync(JsonConvert.DeserializeObject<CreateFriendRabbitRequest>(messageRequest.Data.ToString()));
        }

        private async Task<object> HandleMessageAsync(CreateFriendRabbitRequest createFriendRabbitRequest)
        {
            _logger.LogInformation($"{nameof(CreateFriendRabbitHandler)}.{nameof(HandleMessageAsync)}: Sending request to UserService for method {UserExistsMethod}.");
            var userExistsRabbitResponse = await _friendServiceRabbitRPCService.PublishRabbitMessageWaitForResponseAsync<UserExistsRabbitResponse>(UserExistsMethod, new UserExistsRabbitRequest() { Id = createFriendRabbitRequest.RecieverId });

            var createFriendRabbitResponse = new CreateFriendRabbitResponse();

            if (userExistsRabbitResponse.Exists)
            {
                await _friendRepository.EnsureCreated(createFriendRabbitRequest.RecieverId);
                await _friendRepository.EnsureCreated(createFriendRabbitRequest.SenderId);
                var result = await _friendRepository.UpdateAsync(createFriendRabbitRequest);

                if (result.IsAcknowledged)
                {
                    createFriendRabbitResponse.Successful = true;
                    successfullyCreatedFriendRequestCounter.Inc();
                }
                else
                {
                    unsucccessfulCreatedFriendRequestCounter.Inc();
                }
            }
            else
            {
                unsucccessfulCreatedFriendRequestCounter.Inc();
            }

            return createFriendRabbitResponse;
        }
    }
}
