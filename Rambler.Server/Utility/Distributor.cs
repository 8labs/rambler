namespace Rambler.Server.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Generic pub/sub kinda distribution.
    /// Immediately forwards on the message to all subscribers, no queuing.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being handled</typeparam>
    public class Distributor<TKey, TMessage>
    {
        private readonly Dictionary<TKey, List<Func<TMessage, Task>>> subscriptions = new Dictionary<TKey, List<Func<TMessage, Task>>>();

        public void Subscribe<T>(TKey key, Func<T, Task> action)
            where T : TMessage
        {
            if (!subscriptions.ContainsKey(key))
            {
                subscriptions.Add(key, new List<Func<TMessage, Task>>());
            }

            subscriptions[key].Add((response) =>
            {
                return action((T)response);
            });
        }

        public bool HasSubscription(TKey key)
        {
            return subscriptions.ContainsKey(key);
        }

        public Task Publish(TKey key, TMessage message)
        {
            return Task.WhenAll(subscriptions[key].Select(action => action(message)));
        }
    }
}
