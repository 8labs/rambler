namespace Rambler.Server.Database.Models
{
    using System;

    public class UserIgnore
    {
        public long Id { get; set; }

        public ApplicationUser User { get; set; }
        public Guid UserId { get; set; }

        /// <summary>
        /// The id of the user being ignored
        /// </summary>
        public Guid IgnoreId { get; set; }

        /// <summary>
        /// denormalized for guests, etc.
        /// </summary>
        public string IgnoreNick { get; set; }

        /// <summary>
        /// Used for cleanup
        /// </summary>
        public bool IsGuestIgnore { get; set; }

        public DateTime IgnoredOn { get; set; }
    }
}
