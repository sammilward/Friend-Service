using FriendService.Models;
using FriendService.RabbitMQ.Requests;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FriendService.DataAccess
{
    public interface IFriendRepository
    {
        Task EnsureCreated(string id);
        Task<UpdateResult> UpdateAsync(CreateFriendRabbitRequest CreateFriendRabbitRequest);
        Task UpdateAsync(ResponseToFriendRabbitRequest responseToFriendRabbitRequest);
        Task UpdateAsync(DeleteFriendRabbitRequest deleteFriendRabbitRequest);
        List<string> GetAllIds(GetAllFriendsRabbitRequest getAllFriendsRabbitRequest);
        FriendStatusEnum GetFriendStatus(GetFriendStatusRabbitRequest getFriendStatusRabbitRequest);
    }
}
