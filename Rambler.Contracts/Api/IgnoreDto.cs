namespace Rambler.Contracts.Api
{
    using System;

    public class IgnoreDto
    {
        public long Id { get; set; }

        public Guid UserId { get; set; }

        public string IgnoreNick { get; set; }

        public Guid IgnoreId { get; set; }

        public DateTime IgnoredOn { get; set; }
    }
}
