namespace Rambler.Server.Database.Models
{
    using System;

    public class UserConnection
    {
        public long Id { get; set; }

        public Guid UserId { get; set; }

        public bool IsGuest { get; set; }

        public string Nick { get; set; }

        public string IPAddress { get; set; }

        public DateTime ConnectedOn { get; set; }
    }
}
