declare var gtag: any;

declare namespace Rambler {
    // anope levels - just here so generation works
    enum ChannelModeratorLevels {
        vop = 3,
        aop = 5,
        sop = 10,
        hop = 4
    }
}

declare namespace Rambler.Contracts.Api {
    const enum ModerationLevel {
        Unauthorized = -1000,
        Muted = -10,
        Normal = 1,
        Moderator = 10,
        Admin = 100,
        RoomOwner = 150,
        ServerAdmin = 1000,
    }
    const enum BanLevel {
        Ban = 0,
        Warning = 1,
        Mute = 2,
    }
}

