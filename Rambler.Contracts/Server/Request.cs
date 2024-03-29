﻿namespace Rambler.Contracts.Server
{
    using System;
    using System.Net;

    /// <summary>
    /// An incoming request from the socket.
    /// Data is from the client, but the rest is generated by the socket server can be trusted.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Request<T> : IRequest
    {
        public Request() { }

        public string IPAddress { get; set; }

        public Guid UserId { get; set; }

        public Guid SocketId { get; set; }

        public T Data { get; set; }
    }
}
