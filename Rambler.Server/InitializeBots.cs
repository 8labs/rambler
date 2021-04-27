
namespace Rambler.Server
{
    using Database.Models;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Database;

    public class InitializeBots
    {
        private readonly ApplicationDbContext db;
        private readonly BotService service;

        public InitializeBots(ApplicationDbContext db, BotService service)
        {
            this.db = db;
            this.service = service;
        }

        public async Task LoadBots()
        {
            var bots = await db.Bots
                .Include(b => b.Channels)
                .ToListAsync();

            foreach (var b in bots)
            {
                service.AddBot(b, b.Channels.Select(c => c.ChannelId).ToList());
            }
        }

        public async Task SeedBots()
        {
            var endpoint = "http://example.com:5001/asdf";
            var botname = "TriviaBot";

            var bot = await db.Bots
                .Where(b => b.Name == botname)
                .SingleOrDefaultAsync();

            var room = await db.Channels
                  .Where(c => c.Name == "Trivia")
                  .SingleOrDefaultAsync();

            // db itself probably isn't initialized
            if (room == null) return;

            // already created, just update endpoint
            if (bot != null)
            {
                bot.EndPoint = endpoint;
            }
            else
            {
                var k = await db.Users
                    .Where(u => u.UserName == "k")
                    .SingleOrDefaultAsync();

                // I don't exist!
                if (k == null) return;

                bot = new Bot()
                {
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Name = botname,
                    Description = "A Happy Trivia Bot!",
                    EndPoint = endpoint,
                    IsEnabled = false,
                    Owner = k,
                };

                db.Bots.Add(bot);
                db.BotChannels.Add(new BotChannel()
                {
                    Added = DateTime.UtcNow,
                    BotId = bot.Id,
                    ChannelId = room.Id
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
