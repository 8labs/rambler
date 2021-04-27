namespace Rambler.Test
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Rambler.Server.Database;
    using Rambler.Server.Database.Models;
    using Rambler.Server.WebService.Controllers;
    using System;
    using System.Threading.Tasks;

    [TestClass]
    public class ChannelControllerTests
    {
        private class ChannelControllerFakish : ChannelController
        {
            public ChannelControllerFakish(ApplicationDbContext db, ILogger<ChannelController> logger)
                : base(null, null, db, null, logger) { }

            public ApplicationUser AppUser { get; set; }
            public ApplicationUser LookupUser { get; set; }

            protected override Task<ApplicationUser> GetUser()
            {
                return Task.FromResult(AppUser);
            }

            protected override Task<ApplicationUser> GetUser(Guid? id)
            {
                return Task.FromResult(LookupUser);
            }
        }

        ILogger<ChannelController> log = new Logger<ChannelController>(new LoggerFactory());

        string connectionString = @"Server=127.0.0.1;Port=5432;Database=rambler_auth;User Id = postgres; Password=root;";


        private ApplicationDbContext GetContext()
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseNpgsql(connectionString);
            return new ApplicationDbContext(builder.Options);
        }

        [TestMethod]
        public async Task TestGetChannels()
        {
            using (var db = GetContext())
            {
                var ctrl = new ChannelControllerFakish(db, log);
                await ctrl.GetOwnedChannels();
            }
        }
    }
}
