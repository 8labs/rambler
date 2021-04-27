namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ChannelJoinedResponseProcessor : IResponseProcesor<ChannelJoinedResponse>
    {
        private readonly SocketSubscriptions subs;

        public ChannelJoinedResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ChannelJoinedResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
