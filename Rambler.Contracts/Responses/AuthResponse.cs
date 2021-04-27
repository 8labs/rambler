namespace Rambler.Contracts.Responses
{
    using System;

    [MessageKey("AUTH")]
    public class AuthResponse
    {
        public Guid SocketId { get; set; }

        public Guid UserId { get; set; }

        public string Nick { get; set; }

        public int ConnectionCount { get; set; }
    }
}
