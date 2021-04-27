namespace Rambler.Contracts.Api
{
    using System;

    public class GetSubscriptionMessages
    {
        public string Token { get; set; }
        public Guid Id { get; set; }
        public int Last { get; set; }
    }
}
