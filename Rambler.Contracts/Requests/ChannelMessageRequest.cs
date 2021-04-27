namespace Rambler.Contracts.Requests
{
    using System;

    [MessageKey(KEY)]
    public class ChannelMessageRequest
    {
        public const string KEY = "CHMSG";

        public Guid ChannelId { get; set; }
        public string Message { get; set; }
    }
}
