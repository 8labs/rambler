namespace Rambler.Contracts.Server
{
    using System.Threading.Tasks;

    public interface IRequestPublisher
    {
        Task Publish<T>(Request<T> message);
    }
}
