namespace Rambler.Server.State.Processors
{
    using Contracts.Api;
    using System.Linq;
    using Contracts.Requests;
    using Contracts.Responses;
    using Contracts.Server;
    using Database;
    using Microsoft.Extensions.Logging;
    using State;
    using System;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    public class DirectMessageRequestProcessor : IRequestProcessor<DirectMessageRequest>
    {
        private readonly StateMutator mutator;
        private readonly IResponsePublisher dist;
        private readonly ILogger log;
        private readonly ApplicationDbContext db;

        public DirectMessageRequestProcessor(
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

        public async Task Process(Request<DirectMessageRequest> req)
        {
            var (nick, online) = await mutator.Enqueue(async (state) =>
            {
                // TODO - consolidate this check somewhere.
                // this should happen... somewhere else.  it's a waste of a state mutation
                if (!state.TryGetUser(req.UserId, out var user))
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NotAuthenticated);
                    return (null, false);
                }

                return (user.Nick, state.HasUser(req.Data.UserId));
            });

            if (nick == null) return;

            if (!online)
            {
                // offline handling gets weird
                // to validate that this person ever existed, we'll check the connection log
                var everExisted = await db.UserConnections
                    .Where(c => c.UserId == req.Data.UserId)
                    .AnyAsync();

                // invalid user
                if (!everExisted)
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NoSuchChannel);
                    return;
                };

                // valid user, so let's publish an offline response
                await dist.Publish(new Response<UserOfflineResponse>()
                {
                    Subscription = req.UserId,
                    Data = new UserOfflineResponse()
                    {
                        UserId = req.Data.UserId,
                    }
                });
            }

            var timestamp = DateTime.UtcNow;
            var timestampms = ((DateTimeOffset)timestamp).ToUnixTimeMilliseconds();

            var resp = new Response<DirectMessageResponse>()
            {
                Timestamp = timestampms,
                Subscription = req.Data.UserId,
                Data = new DirectMessageResponse()
                {
                    UserId = req.UserId,
                    Nick = nick,  // may be first contact - always include the nick
                    Message = req.Data.Message,
                    Type = MessageTypes.MESSAGE,
                }
            };

            // Note: this gets published even if the user is offline
            // user can pick up the response on next reconnect (if within the saving window)
            var id = await db.SavePost(
                resp.Subscription,
                resp.Data.UserId,
                timestamp,
                resp.Data.Message,
                nick,
                resp.Data.Type
                );

            resp.Id = id;

            // publish out the DM echo to the main user
            // TODO: look into modding this to use a separate GUID for DMs.. because this is funkay.
            var echo = new Response<DirectMessageResponse>()
            {
                Subscription = req.UserId,
                Timestamp = resp.Timestamp,
                Id = resp.Id,
                Data = new DirectMessageResponse()
                {
                    UserId = req.UserId,
                    EchoUser = req.Data.UserId,
                    Message = req.Data.Message,
                }
            };

            await dist.Publish(resp);
            await dist.Publish(echo);
        }
    }
}
