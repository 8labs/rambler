namespace Rambler.Contracts.Api
{
    public enum ModerationLevel
    {
        Unauthorized = -1000,
        Muted = -10,
        Normal = 0,
        Moderator = 10,
        Admin = 100,
        RoomOwner = 150,
        ServerAdmin = 1000,
        ServerAdminPlus = 1500,
    }
}
