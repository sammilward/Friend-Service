using FriendService.Models;
using FriendService.RabbitMQ.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FriendService.DataAccess
{
    public class MongoFriendRepository : IFriendRepository
    {
        private readonly ILogger<MongoFriendRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<Friend> _collection;

        public MongoFriendRepository(ILogger<MongoFriendRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            _logger.LogInformation($"{nameof(MongoFriendRepository)}: Making connection to mongo server using: {configuration.GetSection("MongoConnection").Value}");
            var client = new MongoClient(configuration.GetSection("MongoConnection").Value);
            _logger.LogInformation($"{nameof(MongoFriendRepository)}: Fetching database: {configuration.GetSection("MongoDatabaseName").Value}");
            var database = client.GetDatabase(configuration.GetSection("MongoDatabaseName").Value);
            _logger.LogInformation($"{nameof(MongoFriendRepository)}: Fetching collection: {configuration.GetSection("MongoCollectionName").Value}");
            _collection = database.GetCollection<Friend>(configuration.GetSection("MongoCollectionName").Value);
        }

        public async Task EnsureCreated(string id)
        {
            var filter = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), id);

            var user = _collection.Find(filter).SingleOrDefault();
            if (user == null)
            {
                var newUser = new Friend() { UserId = id };
                _logger.LogInformation($"{nameof(MongoFriendRepository)}.{nameof(EnsureCreated)}: Adding new friend structure for {id}");
                await _collection.InsertOneAsync(newUser);
            }
        }

        public List<string> GetAllIds(GetAllFriendsRabbitRequest getAllFriendsRabbitRequest)
        {
            List<string> userIds = new List<string>();

            var filterRequestor = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), getAllFriendsRabbitRequest.Id);
            var requestor = _collection.Find(filterRequestor).SingleOrDefault();

            if (getAllFriendsRabbitRequest.Requests.HasValue && getAllFriendsRabbitRequest.Requests.Value)
            {
                userIds = requestor.Requested;
            }
            else if (getAllFriendsRabbitRequest.Requested.HasValue && getAllFriendsRabbitRequest.Requested.Value)
            {
                try
                {
                    var requestedUsers = _collection.Find(x => x.Requested.Any(y => y == getAllFriendsRabbitRequest.Id)).ToList();
                    foreach (var user in requestedUsers)
                    {
                        userIds.Add(user.UserId);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
            else
            {
                userIds = requestor.Friends;
            }

            return userIds;
        }

        public FriendStatusEnum GetFriendStatus(GetFriendStatusRabbitRequest getFriendStatusRabbitRequest)
        {
            var QueryingUserFilter = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), getFriendStatusRabbitRequest.QueryingUser);
            var otherUserFilter = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), getFriendStatusRabbitRequest.OtherUser);

            var queryingUser = _collection.Find(QueryingUserFilter).SingleOrDefault();
            var otherUser = _collection.Find(otherUserFilter).SingleOrDefault();

            if (otherUser.Friends.Contains(getFriendStatusRabbitRequest.QueryingUser))
            {
                return FriendStatusEnum.Friends;
            }
            else if (otherUser.Requested.Contains(getFriendStatusRabbitRequest.QueryingUser))
            {
                return FriendStatusEnum.SentRequest;
            }
            else if (queryingUser.Requested.Contains(getFriendStatusRabbitRequest.OtherUser))
            {
                return FriendStatusEnum.RecievedRequested;
            }
            else return FriendStatusEnum.NotFriends;
        }

        public async Task<UpdateResult> UpdateAsync(CreateFriendRabbitRequest createFriendRabbitRequest)
        {
            _logger.LogInformation($"{nameof(MongoFriendRepository)}.{nameof(UpdateAsync)}: Adding new friend request for {createFriendRabbitRequest.SenderId} and {createFriendRabbitRequest.RecieverId}.");

            var filterReciever = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), createFriendRabbitRequest.RecieverId);

            var reciever = _collection.Find(filterReciever).SingleOrDefault();
            var recievedRequests = reciever.Requested;
            if (!recievedRequests.Contains(createFriendRabbitRequest.SenderId)) recievedRequests.Add(createFriendRabbitRequest.SenderId);

            UpdateDefinition<Friend> recieverUpdateDefinition = Builders<Friend>.Update.Set(nameof(Friend.Requested), recievedRequests);

            return await _collection.UpdateOneAsync(filterReciever, recieverUpdateDefinition);
        }

        public async Task UpdateAsync(ResponseToFriendRabbitRequest responseToFriendRabbitRequest)
        {
            var filterReciever = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), responseToFriendRabbitRequest.RecieverId); //Reciever is person who is responsing to request, Accept or Reject
            var filterSender = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), responseToFriendRabbitRequest.SenderId);

            var reciever = _collection.Find(filterReciever).SingleOrDefault();
            var sender = _collection.Find(filterSender).SingleOrDefault();

            var recieverRequests = reciever.Requested;
            if (!recieverRequests.Contains(responseToFriendRabbitRequest.SenderId)) throw new InvalidOperationException();
            recieverRequests.Remove(responseToFriendRabbitRequest.SenderId);

            var recieverFriends = reciever.Friends;
            var senderFriends = sender.Friends;

            if (responseToFriendRabbitRequest.Accept.HasValue && responseToFriendRabbitRequest.Accept.Value)
            {
                if (!recieverFriends.Contains(responseToFriendRabbitRequest.SenderId)) recieverFriends.Add(responseToFriendRabbitRequest.SenderId);
                if (!senderFriends.Contains(responseToFriendRabbitRequest.RecieverId)) senderFriends.Add(responseToFriendRabbitRequest.RecieverId);
                _logger.LogInformation($"{nameof(MongoFriendRepository)}.{nameof(UpdateAsync)}: Accepting friend request for {responseToFriendRabbitRequest.SenderId} and {responseToFriendRabbitRequest.RecieverId}.");
            }
            else
            {
                _logger.LogInformation($"{nameof(MongoFriendRepository)}.{nameof(UpdateAsync)}: Rejecting friend request for {responseToFriendRabbitRequest.SenderId} and {responseToFriendRabbitRequest.RecieverId}.");
            }

            UpdateDefinition<Friend> recieverUpdateDefinition = Builders<Friend>.Update.Set(nameof(Friend.Requested), recieverRequests).Set(nameof(Friend.Friends), recieverFriends);
            UpdateDefinition<Friend> senderUpdateDefinition = Builders<Friend>.Update.Set(nameof(Friend.Friends), senderFriends);
            await _collection.UpdateOneAsync(filterReciever, recieverUpdateDefinition);
            await _collection.UpdateOneAsync(filterSender, senderUpdateDefinition);
        }

        public async Task UpdateAsync(DeleteFriendRabbitRequest deleteFriendRabbitRequest)
        {
            var filterReciever = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), deleteFriendRabbitRequest.RecieverId);
            var filterSender = Builders<Friend>.Filter.Eq(nameof(Friend.UserId), deleteFriendRabbitRequest.SenderId);

            var reciever = _collection.Find(filterReciever).SingleOrDefault();
            var sender = _collection.Find(filterSender).SingleOrDefault();

            var recieverFriends = reciever.Friends;
            var senderFriends = sender.Friends;
            if (!recieverFriends.Contains(deleteFriendRabbitRequest.SenderId)) throw new InvalidOperationException();
            if (!senderFriends.Contains(deleteFriendRabbitRequest.RecieverId)) throw new InvalidOperationException();
            recieverFriends.Remove(deleteFriendRabbitRequest.SenderId);
            senderFriends.Remove(deleteFriendRabbitRequest.RecieverId);

            UpdateDefinition<Friend> recieverUpdateDefinition = Builders<Friend>.Update.Set(nameof(Friend.Friends), recieverFriends);
            UpdateDefinition<Friend> senderUpdateDefinition = Builders<Friend>.Update.Set(nameof(Friend.Friends), senderFriends);
            await _collection.UpdateOneAsync(filterReciever, recieverUpdateDefinition);
            await _collection.UpdateOneAsync(filterSender, senderUpdateDefinition);
        }
    }
}
