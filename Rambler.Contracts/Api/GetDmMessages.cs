namespace Rambler.Contracts.Api
{
    using System;

    public class GetDmMessages
    {
        public string Token { get; set; }

        /// <summary>
        /// the other user id.
        /// </summary>
        public Guid UserId { get; set; }

        public int Last { get; set; }
    }
}
