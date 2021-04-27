namespace Rambler.Contracts.Requests
{
    using System;

    [MessageKey(KEY)]
    public class ChannelBanRequest
    {
        public const string KEY = "CHBAN";

        public Guid ChannelId { get; set; }

        public Guid UserId { get; set; }

        public string Reason { get; set; }

        public long ExpiresMinutes { get; set; } 
    }
}
