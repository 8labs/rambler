namespace Rambler.Contracts.Api
{
    using System;

    public class ChannelDto
    {
        public Guid Id { get; set; }

        public Guid OwnerId { get; set; }

        public string OwnerNick { get; set; }

        public int UserCount { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastModified { get; set; }

        public DateTime LastActivity { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool AllowGuests { get; set; }

        public bool IsSecret { get; set; }

        public int MaxUsers { get; set; }
    }
}
