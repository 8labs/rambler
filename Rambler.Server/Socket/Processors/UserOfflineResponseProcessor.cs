namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;
    using Contracts.Server;

    public class UserOfflineResponseProcessor : IResponseProcesor<UserOfflineResponse>
    {
        private readonly SocketSubscriptions subs;

        public UserOfflineResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<UserOfflineResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
