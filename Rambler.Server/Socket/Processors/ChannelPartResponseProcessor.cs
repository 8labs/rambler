namespace Rambler.Server.Socket.Processors
{
    using Contracts.Responses;
    using Socket;
    using System.Threading.Tasks;

    public class ChannelPartResponseProcessor : IResponseProcesor<ChannelPartResponse>
    {
        private readonly SocketSubscriptions subs;

        public ChannelPartResponseProcessor(SocketSubscriptions subs)
        {
            this.subs = subs;
        }

        public async Task Process(Response<ChannelPartResponse> response)
        {
            //publish out the message
            await subs.Publish(response);

            //then remove the user from the channel (after everyone has the updates)
            //this allows the parting client to know it needs to cleanup that channel
            subs.UnSubscribeUser(response.Data.UserId, response.Subscription);
        }
    }
}
