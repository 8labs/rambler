namespace Rambler.Server.State.Processors
{
    using Contracts.Api;
    using Contracts.Requests;
    using Contracts.Responses;
    using Contracts.Server;
    using Database;
    using Microsoft.Extensions.Logging;
    using State;
    using System;
    using System.Threading.Tasks;

    public class ChannelMessageRequestProcessor : IRequestProcessor<ChannelMessageRequest>
    {
        private readonly StateMutator mutator;
        private readonly IResponsePublisher dist;
        private readonly ILogger log;
        private readonly ApplicationDbContext db;

        public ChannelMessageRequestProcessor(
            StateMutator mutator,
            IResponsePublisher dist,
            ILogger<ChannelMessageRequestProcessor> log,
            ApplicationDbContext db)
        {
            this.mutator = mutator;
            this.dist = dist;
            this.log = log;
            this.db = db;
        }

        public async Task Process(Request<ChannelMessageRequest> req)
        {
            // likely makes sense to not block for sending the messages
            // only queue the 'can send' check
            // should work similar to dm's
            var user = await mutator.Enqueue(async (state) =>
            {
                if (!state.TryGetUser(req.UserId, out var u))
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NotAuthenticated);
                    return null;
                }

                if (!state.TryGetChannelUserInfo(req.Data.ChannelId, req.UserId, out var info))
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NotInChannel);
                    return null;
                }

                // if info says you're muted, then stay quiet.

                return u;
            });

            if (user == null)
            {
                return; // errored
            }

            var timestamp = DateTime.UtcNow;
            var timestampms = ((DateTimeOffset)timestamp).ToUnixTimeMilliseconds();

            var resp = new Response<ChannelMessageResponse>()
            {
                Subscription = req.Data.ChannelId,
                Timestamp = timestampms,
                Data = new ChannelMessageResponse()
                {
                    UserId = req.UserId,
                    Message = req.Data.Message,
                    Type = MessageTypes.MESSAGE
                }
            };

            var id = await db.SavePost(
                resp.Subscription,
                resp.Data.UserId,
                timestamp,
                resp.Data.Message,
                user.Nick,
                resp.Data.Type
                );

            resp.Id = id;

            await dist.Publish(resp);


        }
    }
}
