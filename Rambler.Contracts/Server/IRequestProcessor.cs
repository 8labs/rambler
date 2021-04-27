namespace Rambler.Contracts.Server
{
    using System.Threading.Tasks;

    /// <summary>
    /// Threadsafe message processor
    /// Handles 'business logic' of the server
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRequestProcessor<T>
    {
        Task Process(Request<T> message);
    }
}
