namespace Rambler.Server.Database.Models
{
    using System;
    using System.Collections.Generic;

    public class Channel
    {
        public Guid Id { get; set; }

        public ApplicationUser Owner { get; set; }

        public Guid OwnerId { get; set;}

        public DateTime Created { get; set; }

        public DateTime LastModified { get; set; }

        public DateTime LastActivity { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool AllowGuests { get; set; }

        public bool IsSecret { get; set; }

        public int MaxUsers { get; set; }

        public List<ChannelBan> Bans { get; set; }

        public List<ChannelModerator> Moderators { get; set; }
    }
}
