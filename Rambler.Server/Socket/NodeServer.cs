namespace Rambler.Server.Socket
{
    using Microsoft.Extensions.Logging;
    using Contracts.Responses;
    using System.Threading.Tasks;
    using Utility;

    public class NodeServer
    {
        private readonly TaskQueue queue = new TaskQueue();
        private readonly ResponseDistributor distrubutor;
        private readonly ILogger log;

        public NodeServer(ResponseDistributor distrubutor, ILogger<NodeServer> log)
        {
            this.distrubutor = distrubutor;
            this.log = log;
        }

        public void AddProcessor<T>(IResponseProcesor<T> processor)
        {
            distrubutor.Subscribe<T>((msg) =>
            {
                queue.Enqueue(() => processor.Process(msg));
                return Task.CompletedTask;
            });
        }

        public Task Start()
        {
            return queue.Start();
        }

        public void Shutdown()
        {
            queue.Stop();
        }
    }
}
