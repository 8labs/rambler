namespace Rambler.Contracts.Api
{
    using System;
    
    public class ChannelBanDto
    {
        public int Id { get; set; }

        public Guid ChannelId { get; set; }
        
        public String ChannelName { get; set; }

        public String CreatedBy { get; set; }

        public Guid UserId { get; set; }

        public string Nick { get; set; }

        public BanLevel Level { get; set; }

        public string Reason { get; set; }

        public DateTime Expires { get; set; }

        public DateTime Created { get; set; }
    }
}
