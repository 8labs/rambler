namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ChannelMessageResponseProcessor : IResponseProcesor<ChannelMessageResponse>
    {
        private readonly SocketSubscriptions subs;

        public ChannelMessageResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ChannelMessageResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
