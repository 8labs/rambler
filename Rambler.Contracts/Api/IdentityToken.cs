namespace Rambler.Contracts.Api
{
    using System;

    /// <summary>
    /// JWT token payload
    /// </summary>
    public class IdentityToken
    {
        public Guid UserId { get; set; }

        public long Expires { get; set; }

        public bool IsGuest { get; set; }

        public bool IsValidated { get; set; }

        public int Level { get; set; }

        public string Nick { get; set; }

        public bool IsExpired()
        {
            return Expires < DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
