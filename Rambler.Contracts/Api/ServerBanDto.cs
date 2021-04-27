namespace Rambler.Contracts.Api
{
    using System;

    public class ServerBanDto
    {
        public int Id { get; set; }

        public Guid? BannedUserId { get; set; }

        public string BannedNick { get; set; }

        public string IPFilter { get; set; }

        public string Reason { get; set; }

        public Guid CreatedById { get; set; }

        public string CreatedByNick { get; set; }

        public DateTime Created { get; set; }

        public DateTime Expires { get; set; }
    }
}
