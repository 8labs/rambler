namespace Rambler.Server.Database.Models
{
    using System;

    public class ChannelPost
    {
        public long Id { get; set; }

        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Denormalized nick name 
        /// (in case user is no longer in channel when history is retrieved)
        /// </summary>
        public string Nick { get; set; }

        /// <summary>
        /// Type of message data
        /// (Notification/bot/etc)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Generally the user sending the message
        /// </summary>
        public Guid Originator { get; set; }

        /// <summary>
        /// Where this is going - may or may not be a channel Id
        /// </summary>
        public Guid Subscription { get; set; }

        /// <summary>
        /// Serialized data of the message
        /// </summary>
        public string Message { get; set; }
    }
}
