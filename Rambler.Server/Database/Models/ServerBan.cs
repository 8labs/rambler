namespace Rambler.Server.Database.Models
{
    using System;

    /// <summary>
    /// Server bans
    /// </summary>
    public class ServerBan
    {
        public int Id { get; set; }

        /// <summary>
        /// Might be a guest ID
        /// </summary>
        public Guid? BannedUserId { get; set; }

        /// <summary>
        /// Just for info
        /// </summary>
        public string BannedNick { get; set; }

        public string IPFilter { get; set; }

        public string Reason { get; set; }

        /// <summary>
        /// denormalized, just used for reference
        /// </summary>
        public Guid CreatedById { get; set; }

        /// <summary>
        /// denormalized, just used for reference
        /// </summary>
        public string CreatedByNick { get; set; }

        public DateTime Created { get; set; }

        public DateTime Expires { get; set; }
    }
}
