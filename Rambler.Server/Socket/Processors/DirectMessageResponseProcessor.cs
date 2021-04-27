namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class DirectMessageResponseProcessor : IResponseProcesor<DirectMessageResponse>
    {
        private readonly SocketSubscriptions subs;

        public DirectMessageResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<DirectMessageResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
