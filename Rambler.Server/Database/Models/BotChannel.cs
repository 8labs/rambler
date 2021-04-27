namespace Rambler.Server.Database.Models
{
    using System;

    public class BotChannel
    {
        public int Id { get; set; }

        public Guid BotId { get; set; }

        public Guid ChannelId { get; set; }

        public DateTime Added { get; set; }
    }
}
