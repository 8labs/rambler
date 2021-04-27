namespace Rambler.Contracts.Responses
{
    using System.Threading.Tasks;

    /// <summary>
    /// Handles responses coming from the statemanager
    /// Might include disconnected users, etc.
    /// Often just passes it on to the sub manager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IResponseProcesor<T>
    {
        Task Process(Response<T> response);
    }


}
