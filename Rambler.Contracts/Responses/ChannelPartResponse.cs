namespace Rambler.Contracts.Responses
{
    using System;

    [MessageKey(KEY)]
    public class ChannelPartResponse
    {
        public const string KEY = "CHPART";

        public Guid UserId { get; set; }
    }
}
