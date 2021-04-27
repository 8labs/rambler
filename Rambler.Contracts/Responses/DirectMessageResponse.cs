namespace Rambler.Contracts.Responses
{
    using System;

    [MessageKey(KEY)]
    public class DirectMessageResponse
    {
        public const string KEY = "DM";

        public Guid UserId { get; set; }

        public string Type { get; set; }

        public string Message { get; set; }

        public Guid? EchoUser { get; set; }

        public string Nick { get; set; }
    }
}
