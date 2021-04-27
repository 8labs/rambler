namespace Rambler.Server
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading.Tasks;

    public static class WebHostExtensions
    {
        /// <summary>
        /// Allows you to run items -after- startup runs
        /// necessary for all the EF madness to still work
        /// </summary>
        /// <param name="host"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IWebHost Initialize(this IWebHost host, Action<IServiceProvider> action)
        {
            using (var scope = host.Services.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;

                try
                {
                    action(serviceProvider);
                }
                catch (Exception ex)
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }
            return host;
        }

        public static IWebHost InitializeService<TService>(this IWebHost host, Func<TService, Task> action)
        {
            Initialize(host, (provider) =>
            {
                var service = provider.GetService<TService>();
                action(service).GetAwaiter().GetResult();
            });

            return host;
        }
    }
}
