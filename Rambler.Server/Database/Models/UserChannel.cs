namespace Rambler.Server.Database.Models
{
    using System;

    public class UserChannel
    {
        public long Id { get; set; }

        public ApplicationUser User { get; set; }

        public Guid UserId { get; set; }

        public Channel Channel { get; set; }

        public Guid ChannelId { get; set; }

        public DateTime JoinedOn { get; set; }
    }
}
