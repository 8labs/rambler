namespace Rambler.Contracts.Api
{
    using System;

    public class ChannelModeratorDto
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string Nick { get; set; }
        public ModerationLevel Level { get; set; }
        public DateTime Created { get; set; }
    }
}
