namespace Rambler.Contracts.Responses
{
    using System;
    using Rambler.Contracts.Api;

    [MessageKey("JOIN")]
    public class JoinResponse
    {
        public Guid ChannelId { get; set; }

        public Guid UserId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int MaxUsers { get; set; }

        public bool IsSecret { get; set; }

        public bool AllowsGuests { get; set; }

        public ModerationLevel Level { get; set; }
    }
}
