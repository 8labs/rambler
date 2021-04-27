namespace Rambler.Server.State
{
    using Contracts.Server;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    public class StateServer
    {
        private readonly IRequestDistributor distributor;
        private readonly ILogger log;
        private readonly IServiceProvider provider;

        public StateServer(IRequestDistributor distributor, ILogger<StateServer> log, IServiceProvider provider)
        {
            this.distributor = distributor;
            this.log = log;
            this.provider = provider;
        }

        public void AddProcessor<T>(Type processorType)
        {
            distributor.Subscribe<T>(async (request) =>
            {
                using (var scope = provider.CreateScope())
                {
                    var processor = (IRequestProcessor<T>)scope.ServiceProvider.GetService(processorType);
                    await Process(processor, (Request<T>)request);
                }
            });
        }

        private async Task Process<T>(IRequestProcessor<T> processor, Request<T> request)
        {
            try
            {
                await processor.Process(request);
            }
            catch (Exception ex)
            {
                //TODO - additional processing as necessary
                //should we distribute back a failure?
                //another error trap on top of that.. blarg!
                //tempted to just ignore - bad message.  deal with it in the log
                log.LogError(0, ex, "Error processing request");
            }
        }
    }
}
