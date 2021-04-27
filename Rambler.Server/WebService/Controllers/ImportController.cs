namespace Rambler.Server.WebService.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using System;
    using Contracts.Api;
    using Database.Models;
    using Database;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [Authorize]
    public class ImportController : ControllerBase
    {
        readonly ILogger logger;
        readonly ApplicationDbContext db;
        readonly UserManager<ApplicationUser> userManager;

        public ImportController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            ILogger<ImportController> logger)
        {
            this.userManager = userManager;
            this.logger = logger;
            this.db = db;
            //throw new NotSupportedException("Import Disabled");
        }

        // [HttpGet]
        // [AllowAnonymous]
        // public async Task<IActionResult> ValidateAllEmails() {
        //     var users = db.Users;

        //     foreach (var user in users) {
        //         user.EmailConfirmed = true;
        //     }

        //     await db.SaveChangesAsync();

        //     return Ok();
        // }

        [HttpPost]
        // [AllowAnonymous]
        public async Task<IActionResult> Anope([FromBody] AnopeImport registrations)
        {

            this.db.UserConnections.RemoveRange(this.db.UserConnections);
            this.db.ChannelModerators.RemoveRange(this.db.ChannelModerators);
            this.db.ChannelBanAddresses.RemoveRange(this.db.ChannelBanAddresses);
            this.db.ChannelBans.RemoveRange(this.db.ChannelBans);
            this.db.Channels.RemoveRange(this.db.Channels);
            this.db.UserRoles.RemoveRange(this.db.UserRoles);
            this.db.Users.RemoveRange(this.db.Users);
            await this.db.SaveChangesAsync();

            await RegisterAnopeUser(registrations.Nicknames);
            await RegisterAnopeChannel(registrations.Channels);
            await RegisterAnopeChannelModerators(registrations.Moderators);

            return Ok();
        }

        [HttpPost]
        // [AllowAnonymous]
        public async Task<IActionResult> RegisterAnopeUser([FromBody] AnopeNicknameRegistration[] registrations)
        {
            string[] admins = new string[]
            {
                "j",
                "k",
                "dv",
                "lyn",
            };

            foreach (AnopeNicknameRegistration registration in registrations)
            {
                var user = new ApplicationUser()
                {
                    UserName = registration.nick,
                    Email = registration.email,
                    RegistrationDate = DateTime.Parse(registration.register_date),
                    LastSeenDate = DateTime.Parse(registration.last_connection_date),
                    EmailConfirmed = true
                };

                if (admins.Contains(registration.nick.ToLower())) {
                    user.Level = ApplicationUser.UserLevel.Admin;
                }

                var result = await userManager.CreateAsync(user, registration.password);
            }

            return Ok();
        }

        [HttpPost]
        // [AllowAnonymous]
        public async Task<IActionResult> RegisterAnopeChannel([FromBody] AnopeChannelRegistration[] registrations)
        {
            foreach (AnopeChannelRegistration registration in registrations)
            {
                if (registration.forbidden)
                {
                    continue;
                }

                var user = await userManager.FindByNameAsync(registration.founder);
                if (user == null)
                {
                    await userManager.FindByNameAsync(registration.successor);
                }
                if (user != null)
                {

                    var channel = new Channel()
                    {
                    Created = DateTime.Parse(registration.time_registered),
                    LastModified = DateTime.UtcNow,
                    LastActivity = DateTime.Parse(registration.last_used),
                    Owner = user,
                    Name = registration.name,
                    Description = registration.last_topic,
                    IsSecret = false,
                    AllowGuests = false,
                    MaxUsers = 250,
                    };

                    if (channel.Name.ToLower() == "lobby" || channel.Name.ToLower() == "the_tavern")
                    {
                        channel.MaxUsers = 250;
                    }

                    await db.Channels.AddAsync(channel);

                    await db.SaveChangesAsync();
                }
            }

            return Ok();
        }

        [HttpPost]
        // [AllowAnonymous]
        public async Task<IActionResult> RegisterAnopeChannelModerators([FromBody] AnopeChannelModerator[] moderators)
        {
            foreach (AnopeChannelModerator moderator in moderators)
            {
                var user = await userManager.FindByNameAsync(moderator.nick);

                var channel = db.Channels.FirstOrDefault(ch => ch.Name == moderator.channel);

                if (user != null && channel != null)
                {
                    var mod_level = ModerationLevel.Moderator;
                    if (moderator.level == ChannelModeratorLevels.sop)
                    {
                        mod_level = ModerationLevel.Admin;
                    }
                    var mod = new ChannelModerator()
                    {
                        Channel = channel,
                        Created = DateTime.UtcNow,
                        Level = mod_level,
                        User = user
                    };

                    await db.ChannelModerators.AddAsync(mod);

                    await db.SaveChangesAsync();
                }
            }

            return Ok();
        }

    }
}