namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class ServerBannedResponseProcessor : IResponseProcesor<ServerBannedResponse>
    {
        private readonly SocketSubscriptions subs;
        private readonly IPublisher publisher;
        private readonly ILogger<ServerBannedResponseProcessor> log;

        public ServerBannedResponseProcessor(SocketSubscriptions subs, IPublisher publisher, ILogger<ServerBannedResponseProcessor> log)
        {
            this.subs = subs;
            this.publisher = publisher;
            this.log = log;
        }

        public async Task Process(Response<ServerBannedResponse> response)
        {
            var socks = subs.Sockets
                .Where(s => s.IsSubscribed(response.Subscription))
                .ToList();

            log.LogInformation("Server ban for {sub}.  Reason: {reason}.  Expires: {expires}.  Socket count: {count}.",
                response.Subscription,
                response.Data.Reason,
                response.Data.Expires,
                socks.Count);

            // reason is in the disconnect message.
            // Let's leave it there for now...
            //await publisher.Publish(response, userSubs);

            foreach (var s in socks)
            {
                await s.Disconnect(
                    System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation,
                    response.Data.Reason);
            }
        }
    }
}
