namespace Rambler.Contracts.Requests
{
    using System;

    [MessageKey(KEY)]
    public class ChannelUsersRequest
    {
        public const string KEY = "CHUSERS";
        public Guid ChannelId { get; set; }
    }
}
