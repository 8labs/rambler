namespace Rambler.Contracts.Requests
{
    using System;

    [MessageKey(KEY)]
    public class JoinRequest
    {
        public const string KEY = "JOIN";

        public Guid? ChannelId { get; set; }

        public string ChannelName { get; set; }
    }
}
