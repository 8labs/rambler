namespace Rambler.Contracts.Api
{
    using System;
    using System.Collections.Generic;

    public class ServerUserInfoDto
    {
        public class UserInfo
        {
            public Guid UserId { get; set; }
            public string Nick { get; set; }
        }

        public class UserChannelInfo
        {
            public Guid ChannelId { get; set; }
            public string Name { get; set; }
            public ModerationLevel Level { get; set; }
        }

        public UserInfo Info { get; set; }

        public int ConnectionCount { get; set; }

        public List<string> IPAddresses { get; set; }

        public List<UserChannelInfo> Channels { get; set; }

        public List<UserInfo> RelatedUsers { get; set; }
    }
}
