namespace Rambler.Contracts.Responses
{
    using System;
    using System.Collections.Generic;

    [MessageKey("CHUSERS")]
    public class ChannelUsersResponse
    {
        public class RoomUser
        {
            public Guid Id { get; set; }
            public string Nick { get; set; }
            public bool IsGuest { get; set; }
            public int ModLevel { get; set; }
        }

        public Guid ChannelId { get; set; }

        public List<RoomUser> Users { get; set; }
    }
}
