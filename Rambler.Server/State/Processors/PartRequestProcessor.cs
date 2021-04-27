namespace Rambler.Server.State.Processors
{
    using Contracts.Requests;
    using Contracts.Responses;
    using Contracts.Server;
    using State;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class PartRequestProcessor : IRequestProcessor<ChannelPartRequest>
    {
        private readonly StateMutator mutator;
        private readonly IResponsePublisher dist;
        private readonly ILogger log;
        public PartRequestProcessor(StateMutator mutator, IResponsePublisher dist, ILogger<PartRequestProcessor> log)
        {
            this.mutator = mutator;
            this.dist = dist;
            this.log = log;
        }

        public async Task Process(Request<ChannelPartRequest> req)
        {
            await mutator.Enqueue(async (state) =>
            {
                var results = state.RemoveChannelUser(req.Data.ChannelId, req.UserId);
                if (results)
                {
                    await dist.Publish(new Response<ChannelPartResponse>()
                    {
                        Subscription = req.Data.ChannelId,
                        Data = new ChannelPartResponse() { UserId = req.UserId }
                    });
                }
                else
                {
                    // send an error response if the client really cares
                    await dist.PublishError(req.UserId, ErrorResponse.ErrorCode.NotInChannel);
                }
            });
        }
    }
}
