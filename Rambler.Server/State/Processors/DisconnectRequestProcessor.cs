namespace Rambler.Server.State.Processors
{
    using Microsoft.Extensions.Logging;
    using Contracts.Requests;
    using Contracts.Responses;
    using Contracts.Server;
    using State;
    using System.Threading.Tasks;
    using System.Linq;

    public class DisconnectRequestProcessor : IRequestProcessor<DisconnectRequest>
    {
        private readonly StateMutator mutator;
        private readonly IResponsePublisher dist;
        private readonly ILogger log;

        public DisconnectRequestProcessor(
            StateMutator mutator,
            IResponsePublisher dist,
            ILogger<DisconnectRequestProcessor> log)
        {
            this.mutator = mutator;
            this.dist = dist;
            this.log = log;
        }

        public async Task Process(Request<DisconnectRequest> req)
        {
            //remove from state and state channels
            await mutator.Enqueue(async (state) =>
            {
                var count = state.GetUserSocketCount(req.UserId);
                if (count <= 1)
                {
                    // user is completely disconnecting.
                    //send out a part message to all the channels this user was sub'd to
                    var subs = state.GetUserChannels(req.UserId).ToList();
                    foreach(var ch in subs)
                    {
                        state.RemoveChannelUser(ch, req.UserId);
                        await dist.Publish(new Response<ChannelPartResponse>()
                        {
                            Subscription = ch,
                            Data = new ChannelPartResponse() { UserId = req.UserId }
                        });
                    }
                }

                // and finally cleanup
                state.RemoveSocket(req.SocketId);
                if (state.GetUserSocketCount(req.UserId) < 1)
                {
                    state.RemoveUser(req.UserId);
                }
            });
        }
    }
}
