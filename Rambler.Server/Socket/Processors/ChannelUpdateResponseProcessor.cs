namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ChannelUpdateResponseProcessor : IResponseProcesor<ChannelUpdateResponse>
    {
        private readonly SocketSubscriptions subs;

        public ChannelUpdateResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ChannelUpdateResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
