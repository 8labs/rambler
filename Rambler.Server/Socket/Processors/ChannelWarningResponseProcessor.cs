namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ChannelWarningResponseProcessor : IResponseProcesor<ChannelWarnedResponse>
    {
        private readonly SocketSubscriptions subs;

        public ChannelWarningResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ChannelWarnedResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
