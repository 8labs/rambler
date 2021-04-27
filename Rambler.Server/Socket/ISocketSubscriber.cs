namespace Rambler.Server.Socket
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.WebSockets;
    using System.Threading.Tasks;

    public interface ISocketSubscriber
    {
        Task OnMessageReceived(Guid from, Guid userId, string address, MemoryStream stream, WebSocketMessageType type);
    }
}