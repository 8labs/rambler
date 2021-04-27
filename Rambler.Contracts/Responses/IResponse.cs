namespace Rambler.Contracts.Responses
{
    using System;

    public interface IResponse
    {
        /// <summary>
        /// What subscription this is responding to
        /// </summary>
        Guid Subscription { get; set; }

        /// <summary>
        /// The unique Message identifier
        /// </summary>
        long Id { get; set; }

        /// <summary>
        /// The outgoing message type
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// Unix timestamp for when this message was processed by the server
        /// </summary>
        long Timestamp { get; set; }
    }
}
