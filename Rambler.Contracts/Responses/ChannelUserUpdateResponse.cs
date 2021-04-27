namespace Rambler.Contracts.Responses
{
    using System;
    using Contracts.Api;

    [MessageKey(KEY)]
    public class ChannelUserUpdateResponse
    {
        public const string KEY = "CHUSERUP";

        public Guid UserId { get; set; }

        public string Nick { get; set; }

        public bool IsGuest { get; set; }

        public ModerationLevel Level { get; set; }
    }
}
