using System;
using System.Collections.Generic;

namespace FriendService.Models
{
    public class Friend
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public List<string> Friends { get; set; } = new List<string>();
        public List<string> Requested { get; set; } = new List<string>();
    }
}
