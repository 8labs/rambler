namespace Rambler.Contracts.Responses
{
    using System;

    [MessageKey(KEY)]
    public class ChannelMessageResponse
    {
        public const string KEY = "CHMSG";

        public Guid UserId { get; set; }

        public string Type { get; set; }

        public string Nick { get; set; }

        public string Message { get; set; }
    }
}
