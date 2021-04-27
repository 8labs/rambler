namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ChannelBannedResponseProcessor : IResponseProcesor<ChannelBannedResponse>
    {
        private readonly SocketSubscriptions subs;

        public ChannelBannedResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ChannelBannedResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
