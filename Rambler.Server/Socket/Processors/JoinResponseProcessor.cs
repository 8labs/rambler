namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class JoinResponseProcessor : IResponseProcesor<JoinResponse>
    {
        private readonly SocketSubscriptions subs;
        public JoinResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<JoinResponse> response)
        {
            // join adds subscriptions for all sockets with the same userid
            subs.SubscribeUser(response.Data.UserId, response.Data.ChannelId);
            await subs.Publish(response);
        }
    }
}
