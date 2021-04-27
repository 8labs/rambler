namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ChannelUsersResponseProcessor : IResponseProcesor<ChannelUsersResponse>
    {
        private readonly SocketSubscriptions subs;

        public ChannelUsersResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ChannelUsersResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
