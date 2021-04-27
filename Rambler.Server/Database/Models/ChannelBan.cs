namespace Rambler.Server.Database.Models
{
    using System;
    using Rambler.Contracts.Api;
    using System.Collections.Generic;

    /// <summary>
    /// Matches either the user Id _or_ the IP filter if set
    /// </summary>
    public class ChannelBan
    {
        public int Id { get; set; }

        public Channel Channel { get; set; }

        public Guid ChannelId { get; set; }

        public Guid? UserId { get; set; }

        public BanLevel Level { get; set; }

        public string Reason { get; set; }

        public string CreatedBy { get; set; }

        public DateTime Expires { get; set; }

        public DateTime Created { get; set; }

        public ICollection<ChannelBanAddress> Addresses { get; set; }
    }
}
