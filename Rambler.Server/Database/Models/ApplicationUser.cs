namespace Rambler.Server.Database.Models
{
    using Microsoft.AspNetCore.Identity;
    using System;
    using System.Collections.Generic;

    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser<Guid>
    {
        public enum UserLevel
        {
            Normal = 0,
            Admin = 1000,
            AdminPlus = 1500,
        }

        public int MaxRooms { get; set; } = 1;
        public DateTime RegistrationDate { get; set; }
        public DateTime LastSeenDate { get; set; }
        public UserLevel Level { get; set; }
        public List<Channel> Channels { get; set; }
    }
}
