namespace Rambler.Contracts.Responses
{
    using System;
    using System.Collections.Generic;

    [MessageKey(KEY)]
    public class ChannelWarnedResponse
    {
        public const string KEY = "CHWARN";

        public class Warning
        {
            public Guid UserId { get; set; }

            public string Reason { get; set; }

            public DateTime Expires { get; set; }
        }

        public IEnumerable<Warning> Warnings { get; set; }
    }
}
