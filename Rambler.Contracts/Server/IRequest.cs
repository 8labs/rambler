namespace Rambler.Contracts.Server
{
    using System;

    public interface IRequest
    {
        ///// <summary>
        ///// Server that the socket/user originated from
        ///// </summary>
        //Guid Server { get; set; }

        /// <summary>
        /// Socket/user source of the message
        /// </summary>
        Guid UserId { get; set; }

        /// <summary>
        /// The user assigned to this socket
        /// </summary>
        Guid SocketId { get; set; }
    }
}
