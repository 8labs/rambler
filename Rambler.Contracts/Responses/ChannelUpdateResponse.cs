namespace Rambler.Contracts.Responses
{
    using System;

    [MessageKey(KEY)]
    public class ChannelUpdateResponse
    {
        public const string KEY = "CHUPDATE";

        public string Name { get; set; }

        public string Description { get; set; }

        public bool AllowsGuests { get; set; }

        public bool IsSecret { get; set; }

        public int MaxUsers { get; set; }

        public DateTime LastModified { get; set; }
    }
}
