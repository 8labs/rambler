namespace Rambler.Server.Socket
{
    using Contracts.Server;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading.Tasks;
    using Utility;
    using Rambler.Contracts;

    public class JsonSocketSubscriber : ISocketSubscriber
    {
        private readonly IRequestPublisher publisher;
        private readonly ILogger log;

        private readonly Dictionary<string, Func<Guid, Guid, string, string, Task>> converters = new Dictionary<string, Func<Guid, Guid, string, string, Task>>();

        public JsonSocketSubscriber(IRequestPublisher publisher, ILogger<JsonSocketSubscriber> log)
        {
            this.publisher = publisher;
            this.log = log;
        }

        public void AddMessageType<T>(string key = null)
        {
            if (key == null)
            {
                key = typeof(T).GetAttribute<MessageKey>().Key;
            }

            if (key == null)
            {
                throw new ArgumentNullException("Key is required.");
            }

            //creates a function to generate messages for types
            //the key is pulled from the attribute or passed in
            converters.Add(key, (originator, userId, ip, value) =>
            {
                var msg = JsonConvert.DeserializeObject<T>(value);
                var req = new Request<T>()
                {
                    IPAddress = ip,
                    UserId = userId,
                    SocketId = originator,
                    Data = msg,
                };

                return publisher.Publish(req);
            });
        }

        public Task OnMessageReceived(Guid from, Guid userid, string address, MemoryStream stream, WebSocketMessageType type)
        {
            log.LogDebug("Starting distribute.");

            if (type != WebSocketMessageType.Text)
            {
                throw new Exception("Unsupported data type");
            }

            //not sure under what circumstances this can fail?
            if (!stream.TryGetBuffer(out var data))
            {
                throw new Exception("Something went horridly wrong");
            }

            var content = Encoding.UTF8.GetString(data.Array, 0, data.Count);

            //temporary hack to get content type
            var msgStart = content.IndexOf("{");
            if (msgStart < 1)
            {
                throw new Exception("Invalid message");
            }

            //verify that the content type is validish
            var msgType = content.Substring(0, msgStart);
            if (!converters.ContainsKey(msgType))
            {
                throw new Exception("Unsupported message type: " + msgType);
            }

            log.LogDebug("distribute: {content}", content);

            return converters[msgType](from, userid, address, content.Substring(msgStart));
        }


    }
}
