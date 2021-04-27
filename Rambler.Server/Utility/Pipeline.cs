namespace Rambler.Server.Utility
{
    using System;
    using System.IO;
    using System.Net.WebSockets;
    using System.Threading.Tasks;
    using Socket;

    public delegate Task SocketPipelineDelegate(SocketHandler from, MemoryStream stream, WebSocketMessageType type);

    public delegate SocketPipelineDelegate SocketPipeMiddleware(SocketPipelineDelegate next);

    /// <summary>
    /// putzing with the idea of a generic pipeline for handling incoming requests
    /// throttles, etc...
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Pipeline<T>
    {
        public T Pipe { get; private set; }

        public Pipeline<T> Use(Func<T, T> middleware)
        {
            Pipe = middleware(Pipe);
            return this;
        }
    }

    public class SocketPipe : Pipeline<SocketPipelineDelegate>
    {
        public Task OnMessageReceived(SocketHandler from, MemoryStream stream, WebSocketMessageType type)
        {
            return Pipe(from, stream, type);
        }

        public void Test()
        {
            this.Use(next => (from, stream, type) =>
            {
                //throttling fun?
                return next(from, stream, type);
            });
        }
    }
}
