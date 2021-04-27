namespace Rambler.Contracts.Responses
{
    using System;
    using System.Collections.Generic;

    [MessageKey("LIST")]
    public class ListResponse
    {
        public class ListChannel
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int MaxUsers { get; set; }
            public bool IsSecret { get; set; }
            public bool AllowsGuests { get; set; }
            public int UserCount { get; set; }
        }

        public List<ListChannel> Channels { get; set; }
    }
}
