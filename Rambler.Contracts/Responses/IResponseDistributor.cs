namespace Rambler.Contracts.Responses
{
    using System;
    using System.Threading.Tasks;

    public interface IResponseDistributor
    {
        void Subscribe<T>(Func<Response<T>, Task> action);
    }
}
