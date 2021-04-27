namespace Rambler.Server.Database.Models
{
    using System;
    using Rambler.Contracts.Api;

    public class ChannelModerator
    {
        public int Id { get; set; }

        public Channel Channel { get; set; }

        public Guid ChannelId { get; set; }

        public ApplicationUser User { get; set; }

        public Guid UserId { get; set; }

        public ModerationLevel Level { get; set; }

        public DateTime Created { get; set; }
    }
}
