namespace Rambler.Contracts.Responses
{
    using System;

    [MessageKey(KEY)]
    public class ServerBannedResponse
    {
        public const string KEY = "SRVBAN";

        public string Reason { get; set; }

        public DateTime Expires { get; set; }
    }
}
