namespace Rambler.Contracts.Responses
{
    using System;
    using Contracts.Api;

    [MessageKey(KEY)]
    public class ChannelBannedResponse
    {
        public const string KEY = "CHBAN";

        public Guid UserId { get; set; }

        public string Reason { get; set; }

        public BanLevel Level { get; set; }

        public DateTime Expires { get; set; }

        public String ChannelName { get; set; }

        public String ModeratorNick { get; set; }
    }
}
