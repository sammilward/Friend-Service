using System;

namespace FriendService.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BirthCountry { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string CurrentCountry { get; set; }
        public string CurrentCity { get; set; }
        public DateTime DOB { get; set; }
        public bool Male { get; set; }
    }
}
