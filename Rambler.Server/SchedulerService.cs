namespace Rambler.Server
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class SchedulerService : HostedService
    {
        protected SchedulerService()
        {
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //await ExecuteOnceAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }
}
