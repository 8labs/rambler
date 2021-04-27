namespace Rambler.Server.Socket
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Contracts.Responses;
    using Utility;
    using Contracts;

    /// <summary>
    /// Handles publishing outgoing messages
    /// Serialization, etc.
    /// </summary>
    public class JsonPublisher : IPublisher
    {
        public Task Publish<T>(Response<T> response, IEnumerable<SocketHandler> subscribers)
        {
            // use some magic to grab the key
            // this is pretty putzy, should be cleaned up
            // might make more sense to 'collapse' the response types
            var type = typeof(T).GetAttribute<MessageKey>();
            response.Type = type.Key;

            //serialize the entire response, including subscription
            //helpful for the client to put in the right bucket
            //can optimize per message later or something
            var message = JsonConvert.SerializeObject(response);

            var data = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));

            return Task.WhenAll(subscribers
                .Select(s => s.Publish(data, System.Net.WebSockets.WebSocketMessageType.Text)));
        }
    }
}
