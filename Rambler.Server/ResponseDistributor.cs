namespace Rambler.Server
{
    using Contracts.Responses;
    using Database;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;
    using Utility;

    /// <summary>
    /// Hack for passing around the messages to appropriate response processors.
    /// Eventually publisher and distributor will be running in different processes (servers)
    /// </summary>
    public class ResponseDistributor : IResponsePublisher, IResponseDistributor
    {
        private readonly Distributor<Type, IResponse> distributor = new Distributor<Type, IResponse>();

        private readonly ILogger log;

        public ResponseDistributor(ILogger<ResponseDistributor> log)
        {
            this.log = log;
        }

        public void Subscribe<T>(Func<Response<T>, Task> action)
        {
            var key = typeof(T);
            log.LogDebug("Response subscription for: {key} ", key.Name);
            distributor.Subscribe(key, action);
        }

        public async Task Publish<T>(Response<T> message)
        {
            var key = typeof(T);
            if (!distributor.HasSubscription(key))
            {
                throw new Exception("Unsupported response type: " + key);
            }

            log.LogDebug("Response: {key}", key.Name);


            // TODO - this should probably happen somewhere else...
            // Possibly replace with a snowflake ID?
            if (message.Timestamp <= 0)
            {
                message.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            await distributor.Publish(key, message);
        }


    }

}
