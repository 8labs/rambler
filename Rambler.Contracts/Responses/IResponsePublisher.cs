namespace Rambler.Contracts.Responses
{
    using System.Threading.Tasks;

    public interface IResponsePublisher
    {
        Task Publish<T>(Response<T> message);
    }
}
