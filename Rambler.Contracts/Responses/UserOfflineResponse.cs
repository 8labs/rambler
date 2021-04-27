namespace Rambler.Contracts.Responses
{
    using System;

    [MessageKey("UOFF")]
    public class UserOfflineResponse
    {
        public Guid UserId { get; set; }
    }
}
