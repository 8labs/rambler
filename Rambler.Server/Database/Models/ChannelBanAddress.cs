namespace Rambler.Server.Database.Models
{
    public class ChannelBanAddress
    {
        public int Id { get; set; }

        public int BanId { get; set; }

        public string IPFilter { get; set; }
    }
}
