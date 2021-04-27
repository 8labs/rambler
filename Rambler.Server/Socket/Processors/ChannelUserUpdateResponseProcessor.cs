namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ChannelUserUpdateResponseProcessor : IResponseProcesor<ChannelUserUpdateResponse>
    {
        private readonly SocketSubscriptions subs;

        public ChannelUserUpdateResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ChannelUserUpdateResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
