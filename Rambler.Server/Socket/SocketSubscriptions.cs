namespace Rambler.Server.Socket
{
    using Contracts.Responses;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// handles socket subscriptions
    /// </summary>
    public class SocketSubscriptions
    {
        public IEnumerable<SocketHandler> Sockets
        {
            get
            {
                return lookup.Values;
            }
        }

        private readonly ConcurrentDictionary<Guid, SocketHandler> lookup = new ConcurrentDictionary<Guid, SocketHandler>();
        private readonly IPublisher publisher;

        public SocketSubscriptions(IPublisher publisher)
        {
            this.publisher = publisher;
        }

        private IEnumerable<SocketHandler> GetForUserId(Guid id)
        {
            return Sockets.Where(s => s.Identity.UserId == id);
        }

        /// <summary>
        /// add a new socket tracked by the subscription handler
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public bool Add(SocketHandler socket)
        {
            return lookup.TryAdd(socket.Id, socket);
        }

        /// <summary>
        /// Removes the specific socket
        /// </summary>
        /// <param name="socket"></param>
        public void Remove(SocketHandler socket)
        {
            lookup.TryRemove(socket.Id, out var trash);
        }

        /// <summary>
        /// Removes a subscription for the specified subscriber socket
        /// </summary>
        /// <param name="socketId"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public void UnSubscribe(Guid socketId, Guid subscription)
        {
            if (lookup.TryGetValue(socketId, out var socket))
            {
                socket.UnSubscribe(subscription);
            }
        }

        /// <summary>
        /// adds a subscription to a socket
        /// </summary>
        /// <param name="socketId"></param>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public void Subscribe(Guid socketId, Guid subscription)
        {
            if (lookup.TryGetValue(socketId, out var socket))
            {
                socket.Subscribe(subscription);
            }
        }

        /// <summary>
        /// Adds a subscription to all sockets with the same userid.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="subscription"></param>
        public void SubscribeUser(Guid userId, Guid subscription)
        {
            foreach (var socket in GetForUserId(userId))
            {
                socket.Subscribe(subscription);
            }
        }

        /// <summary>
        /// removes subscription from all sockets with the same userid
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="subscription"></param>
        public void UnSubscribeUser(Guid userId, Guid subscription)
        {
            foreach (var socket in GetForUserId(userId))
            {
                socket.UnSubscribe(subscription);
            }
        }

        public Task Publish<T>(Response<T> response)
        {
            //This seems to be performant enough for the time being.
            //On local machine: 10k sockets can be searched in 3ms or so.
            //Guid lookups are stupid fast.
            //In the future it may require a subscript to sockets index.

            // TODO : might need to think about per-socket queues for outgoing messages
            // not sure, but pretty sure one bad socket could slow down an entire server.
            return publisher.Publish(
                response,
                Sockets.Where(s => s.IsSubscribed(response.Subscription)));
        }
    }
}
