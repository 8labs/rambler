namespace Rambler.Contracts.Requests
{
    using System;

    [MessageKey(KEY)]
    public class DirectMessageRequest
    {
        public const string KEY = "DM";

        public Guid UserId { get; set; }
        public string Message { get; set; }
    }
}
