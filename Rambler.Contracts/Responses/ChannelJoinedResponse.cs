namespace Rambler.Contracts.Responses
{
    using System;
    using Contracts.Api;

    [MessageKey(KEY)]
    public class ChannelJoinedResponse
    {
        public const string KEY = "CHJOIN";

        public Guid UserId { get; set; }

        public string Nick { get; set; }

        public bool IsGuest { get; set; }

        public ModerationLevel Level { get; set; }
    }
}
