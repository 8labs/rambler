namespace Rambler.Contracts.Requests
{
    using System;

    [MessageKey(KEY)]
    public class ChannelPartRequest
    {
        public const string KEY = "CHPART";

        public Guid ChannelId { get; set; }
    }
}
