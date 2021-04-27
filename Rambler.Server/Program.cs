namespace Rambler.Server
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using System.Net;

    class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args)
                // these happen outside buildwebhost so EF keeps working
                // https://stackoverflow.com/questions/45148389/how-to-seed-in-entity-framework-core-2
                .InitializeService<InitializeBots>(b => b.SeedBots())
                .InitializeService<InitializeBots>(b => b.LoadBots())
                .Run();
        }

        public static IWebHost BuildWebHost(string[] args) => new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

    }
}