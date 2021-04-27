namespace Rambler.Server.Socket
{
    using Contracts.Responses;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPublisher
    {
        Task Publish<T>(Response<T> response, IEnumerable<SocketHandler> subscribers);
    }
}