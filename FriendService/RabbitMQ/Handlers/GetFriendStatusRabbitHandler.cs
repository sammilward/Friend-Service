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
    public class GetFriendStatusRabbitHandler : RabbitMessageHandler
    {
        protected override string MethodCanHandle => "GetFriendStatus";

        private const string UserExistsMethod = "UserExists";

        private readonly ILogger<GetFriendStatusRabbitHandler> _logger;
        private readonly IFriendServiceRabbitRPCService _friendServiceRabbitRPCService;
        private readonly IFriendRepository _friendRepository;

        private readonly Counter rabbitMessagesRecievedCounter = Metrics.CreateCounter("GetFriendStatusRabbitMessagesRecieved", "Number of rabbit messages recieved to GetFriendStatus handler");
        private readonly Counter successfullyGetFriendStatusRequestCounter = Metrics.CreateCounter("successfullyGetFriendStatusRequest", "Number of successfully Get friend status requests");
        private readonly Counter unsucccessfulGetFriendStatusRequestCounter = Metrics.CreateCounter("unsucccessfulGetFriendStatusRequest", "Number of unsuccessfull Get friend status requests");

        public GetFriendStatusRabbitHandler(ILogger<GetFriendStatusRabbitHandler> logger, IFriendServiceRabbitRPCService friendServiceRabbitRPCService, IFriendRepository friendRepository)
        {
            _logger = logger;
            _friendServiceRabbitRPCService = friendServiceRabbitRPCService;
            _friendRepository = friendRepository;
        }

        protected override async Task<object> ConvertMessageAndHandle(RabbitMessageRequestModel messageRequest)
        {
            rabbitMessagesRecievedCounter.Inc();
            _logger.LogInformation($"{nameof(GetAllFriendRabbitHandler)}.{nameof(ConvertMessageAndHandle)}: Converting message.");

            return await HandleMessageAsync(JsonConvert.DeserializeObject<GetFriendStatusRabbitRequest>(messageRequest.Data.ToString()));
        }

        private async Task<object> HandleMessageAsync(GetFriendStatusRabbitRequest getFriendStatusRabbitRequest)
        {
            _logger.LogInformation($"{nameof(CreateFriendRabbitHandler)}.{nameof(HandleMessageAsync)}: Sending request to UserService for method {UserExistsMethod}.");
            var userExistsRabbitResponse = await _friendServiceRabbitRPCService.PublishRabbitMessageWaitForResponseAsync<UserExistsRabbitResponse>(UserExistsMethod, new UserExistsRabbitRequest() { Id = getFriendStatusRabbitRequest.OtherUser });

            var getFriendStatusRabbitResponse = new GetFriendStatusRabbitResponse();

            if (userExistsRabbitResponse.Exists)
            {
                await _friendRepository.EnsureCreated(getFriendStatusRabbitRequest.QueryingUser);
                await _friendRepository.EnsureCreated(getFriendStatusRabbitRequest.OtherUser);
                var friendStatus = _friendRepository.GetFriendStatus(getFriendStatusRabbitRequest);
                getFriendStatusRabbitResponse.FriendStatus = friendStatus;
                getFriendStatusRabbitResponse.Successful = true;
                successfullyGetFriendStatusRequestCounter.Inc();
            }
            else unsucccessfulGetFriendStatusRequestCounter.Inc();
            return getFriendStatusRabbitResponse;
        }
    }
}
