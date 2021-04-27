namespace Rambler.Server.State.Processors
{
    using Contracts.Requests;
    using Contracts.Responses;
    using Contracts.Server;
    using State;
    using System.Linq;
    using System.Threading.Tasks;

    public class ChannelUsersRequestProcessor : IRequestProcessor<ChannelUsersRequest>
    {
        private StateMutator mutator;
        private IResponsePublisher dist;

        public ChannelUsersRequestProcessor(StateMutator mutator, IResponsePublisher dist)
        {
            this.mutator = mutator;
            this.dist = dist;
        }

        public async Task Process(Request<ChannelUsersRequest> req)
        {
            await mutator.Enqueue(async (state) =>
            {
                if (!state.IsUserInChannel(req.Data.ChannelId, req.UserId))
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NotInChannel);
                    return;
                }

                var chUsers = state.GetRoomUsers(req.Data.ChannelId);
                await dist.Publish(new Response<ChannelUsersResponse>()
                {
                    Subscription = req.SocketId,
                    Data = new ChannelUsersResponse()
                    {
                        ChannelId = req.Data.ChannelId,
                        Users = chUsers,
                    }
                });
            });
        }
    }
}
