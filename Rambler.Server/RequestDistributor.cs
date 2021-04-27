namespace Rambler.Server
{
    using Microsoft.Extensions.Logging;
    using Rambler.Contracts.Server;
    using System;
    using System.Threading.Tasks;
    using Utility;

    /// <summary>
    /// Handles distributing received messages to appropriate handlers
    /// For in-process message handling (no service barrier)
    /// This fakes a disconnect/barrier between the socket and state servers
    /// Light wrapper around the <see cref="Distributor{TKey, TMessage}"/>
    /// </summary>
    public class RequestDistributor : IRequestDistributor, IRequestPublisher
    {
        private readonly Distributor<Type, IRequest> distributor = new Distributor<Type, IRequest>();
        private readonly ILogger log;

        public RequestDistributor(ILogger<RequestDistributor> logger)
        {
            log = logger;
        }

        public void Subscribe<T>(Func<IRequest, Task> action)
        {
            var key = typeof(T);
            log.LogDebug("Request subscription for: " + key.Name);
            distributor.Subscribe(key, action);
        }

        public Task Publish<T>(Request<T> message)
        {
            var key = typeof(T);
            if (!distributor.HasSubscription(key))
            {
                throw new Exception("Unsupported request type: " + key.FullName);
            }

            log.LogDebug("Request: {key}", key.Name);

            // note, we're ignoring the task returned by the state server
            // normally it wouldn't be available
            // This is 'faking' that disconnect between the services
            distributor.Publish(key, message);

            // fire and forget.  When servers are actually disconnected, this would represent a successful send
            return Task.CompletedTask;
        }
    }

}
