namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class AuthResponseProcessor : IResponseProcesor<AuthResponse>
    {
        private readonly SocketSubscriptions subs;

        public AuthResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<AuthResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
