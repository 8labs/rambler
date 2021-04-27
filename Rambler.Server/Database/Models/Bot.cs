namespace Rambler.Server.Database.Models
{
    using System;
    using System.Collections.Generic;

    public class Bot
    {
        public Guid Id { get; set; }

        public bool IsEnabled { get; set; }

        public ApplicationUser Owner { get; set; }

        public Guid OwnerId { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastModified { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Secret used in both directions.  
        /// Bot can validate post originated from here.
        /// And rambler can use to validate the bot posts.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Path to post to
        /// </summary>
        public string EndPoint { get; set; }

        public List<BotChannel> Channels { get; set; }

        // eventually this would be used to track what events to send
        //public IEnumerable<string> SubscribedEvents { get; set; }

        // and what permissions the bot has
        //public IEnumerable<string> ApiPermissions { get; set; }
    }
}
