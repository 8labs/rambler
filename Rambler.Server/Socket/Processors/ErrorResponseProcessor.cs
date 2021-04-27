namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ErrorResponseProcessor : IResponseProcesor<ErrorResponse>
    {
        private readonly SocketSubscriptions subs;

        public ErrorResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ErrorResponse> response)
        {
            await subs.Publish(response);
        }
    }
}
