namespace Rambler.Contracts.Api {
    public enum ChannelModeratorLevels {
        vop = 3,
        aop = 5,
        sop = 10,
        hop = 4
    }
    public class AnopeChannelRegistration {
        public string name { get; set; }
        public string founder { get; set; }
        public string successor { get; set; }
        public string time_registered { get; set; }
        public string last_used { get; set; }
        public string last_topic { get; set; }
        public bool forbidden { get; set; }
        public string forbidreason { get; set; }
    }

    public class AnopeChannelModerator {
        public ChannelModeratorLevels level { get; set; }
        public string nick { get; set; }
        public string channel { get; set; }
        public string last_seen { get; set; }
    }

    public class AnopeNicknameRegistration {
        public string nick { get; set; }
        public string email { get; set; }
        public string register_date { get; set; }
        public string last_connection_date { get; set; }
        public string password { get; set; }
    }

    public class AnopeImport {
        public AnopeNicknameRegistration[] Nicknames { get; set; }
        public AnopeChannelRegistration[] Channels { get; set; }
        public AnopeChannelModerator[] Moderators { get; set; }
    }
}