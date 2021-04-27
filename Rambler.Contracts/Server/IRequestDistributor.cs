namespace Rambler.Contracts.Server
{
    using System;
    using System.Threading.Tasks;

    public interface IRequestDistributor
    {
        void Subscribe<T>(Func<IRequest, Task> action);
    }
}
