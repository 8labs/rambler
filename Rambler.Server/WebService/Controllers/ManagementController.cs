namespace Rambler.Server.WebService.Controllers
{
    using Contracts.Api;
    using Contracts.Responses;
    using Database;
    using Database.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using State;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Authorize]
    public class ManagementController : ControllerBase
    {
        readonly ILogger logger;
        readonly ApplicationDbContext db;
        readonly UserManager<ApplicationUser> userManager;
        readonly StateMutator mutator;
        readonly IResponsePublisher dist;

        public ManagementController(
            UserManager<ApplicationUser> userManager,
            StateMutator mutator,
            ApplicationDbContext db,
            IResponsePublisher dist,
            ILogger<ChannelController> logger)
        {
            this.userManager = userManager;
            this.logger = logger;
            this.db = db;
            this.mutator = mutator;
            this.dist = dist;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserSockets(Guid userId)
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.Admin)
            {
                return Unauthorized();
            }

            var sockets = await mutator.Enqueue((state) =>
            {
                return state
                     .GetUserSockets(userId)
                     .ToList();
            });

            return Ok(sockets);
        }

        [HttpGet]
        public async Task<IActionResult> GetServerBans()
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.Admin)
            {
                return Unauthorized();
            }

            var bans = await db.ServerBans
                .ToListAsync();

            return Ok(bans.Select(FromBan));
        }

        [HttpPost]
        public async Task<IActionResult> AddServerBanForUser([FromBody] ServerBanDto ban)
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.Admin)
            {
                return Unauthorized();
            }

            var (uid, nick) = await LookupUser(ban.BannedUserId, ban.BannedNick);
            if (!uid.HasValue)
            {
                return BadRequest("User not found.");
            }

            // get addresses for the ban user
            var addys = await db.UserConnections
                .Where(u => u.UserId == uid)
                .Select(u => u.IPAddress)
                .Distinct()
                .ToListAsync();

            var bans = new List<ServerBan>();

            foreach (var ip in addys)
            {
                var b = new ServerBan()
                {
                    BannedNick = nick,
                    BannedUserId = uid,
                    Created = DateTime.UtcNow,
                    CreatedById = user.Id,
                    CreatedByNick = user.UserName,
                    Expires = ban.Expires,
                    IPFilter = ip,
                    Reason = ban.Reason,
                };
                db.ServerBans.Add(b);
                bans.Add(b);
            }

            if (bans.Count < 1)
            {
                // user has no connection history
                // create an empty
                var b = new ServerBan()
                {
                    BannedNick = nick,
                    BannedUserId = uid,
                    Created = DateTime.UtcNow,
                    CreatedById = user.Id,
                    CreatedByNick = user.UserName,
                    Expires = ban.Expires,
                    Reason = ban.Reason,
                };
                db.ServerBans.Add(b);
                bans.Add(b);
            }

            await db.SaveChangesAsync();

            foreach (var b in bans)
            {
                await EnforceBan(b, user.Level);
            }

            return Ok(bans.Select(FromBan));
        }

        [HttpPost]
        public async Task<IActionResult> AddServerBan([FromBody] ServerBanDto ban)
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.Admin)
            {
                return Unauthorized();
            }

            var newBan = new ServerBan()
            {
                BannedNick = ban.BannedNick,
                BannedUserId = ban.BannedUserId,
                Created = DateTime.UtcNow,
                CreatedById = user.Id,
                CreatedByNick = user.UserName,
                Expires = ban.Expires,
                IPFilter = ban.IPFilter,
                Reason = ban.Reason,
            };
            db.ServerBans.Add(newBan);

            await EnforceBan(newBan, user.Level);

            return Ok(FromBan(newBan));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateServerBan([FromBody] ServerBanDto ban)
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.Admin)
            {
                return Unauthorized();
            }

            var existing = await db.ServerBans
                .Where(b => b.Id == ban.Id)
                .SingleOrDefaultAsync();

            if (existing == null)
            {
                return NotFound("Cound not find ban with id: " + ban.Id);
            }

            existing.BannedNick = ban.BannedNick;
            existing.BannedUserId = ban.BannedUserId;
            existing.Created = DateTime.UtcNow;
            existing.CreatedById = user.Id;
            existing.CreatedByNick = user.UserName;
            existing.Expires = ban.Expires;
            existing.IPFilter = ban.IPFilter;
            existing.Reason = ban.Reason;

            db.ServerBans.Update(existing);

            await db.SaveChangesAsync();

            await EnforceBan(existing, user.Level);

            return Ok(FromBan(existing));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveServerBan(int id)
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.Admin)
            {
                return Unauthorized();
            }

            var existing = await db.ServerBans
                .Where(b => b.Id == id)
                .SingleOrDefaultAsync();

            if (existing == null)
            {
                return Ok(); // good enough
            }

            db.ServerBans.Remove(existing);

            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetUserInfo(string nick)
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.Admin)
            {
                return Unauthorized();
            }

            var info = new ServerUserInfoDto();
            info.Info = new ServerUserInfoDto.UserInfo();

            var (uid, uname) = await LookupUser(nick);
            if (!uid.HasValue)
            {
                info.Info.Nick = "Not found: " + nick;
                return Ok(info);
            }

            info.Info.UserId = uid.Value;
            info.Info.Nick = uname;            

            // get sockets, room info
            var socks = await mutator.Enqueue((state) => state.GetUserSockets(uid.Value).ToList());
            info.ConnectionCount = socks.Count;
            if (socks.Count > 0)
            {
                info.Channels = await mutator.Enqueue((state) => state.GetUserChannelInfos(uid.Value));
            }

            // get all the addresses for this userid
            var addys = await db.UserConnections
                .Where(u => u.UserId == uid.Value)
                .Select(u => u.IPAddress)
                .Distinct()
                .ToListAsync();

            info.IPAddresses = addys;

            // now get any users sharing that address
            var related = await db.UserConnections
                .Where(c => addys.Contains(c.IPAddress))
                .OrderByDescending(c => c.Id)
                .Select(c => new ServerUserInfoDto.UserInfo()
                {
                    Nick = c.Nick,
                    UserId = c.UserId
                })
                .Distinct()
                .ToListAsync();

            info.RelatedUsers = related
                .Where(c => c.Nick != null && c.UserId != info.Info.UserId)
                .ToList();

            return Ok(info);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAdmin(bool up, string nick)
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.Admin)
            {
                return Unauthorized();
            }

            var targetUser = string.IsNullOrWhiteSpace(nick)
                ? user
                : await userManager.FindByNameAsync(nick);

            if (targetUser != user && targetUser.Level >= user.Level)
            {
                return BadRequest("Cannot moderate users with same authorization level.");
            }

            await PublishAdminToggle(targetUser.Id, up, (ModerationLevel)targetUser.Level);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ResetUserPassword([FromBody] UserResetDto reset)
        {
            var user = await GetUser();

            if (user == null || user.Level < ApplicationUser.UserLevel.AdminPlus)
            {
                return Unauthorized();
            }

            if (reset.NewPassword != reset.VerifyNewPassword)
            {
                return BadRequest("passwords don't match");
            }

            var target = await GetUser(reset.UserId);
            if (target == null)
            {
                return BadRequest("User not found");
            }

            var code = await userManager.GeneratePasswordResetTokenAsync(target);
            var res = await userManager.ResetPasswordAsync(target, code, reset.NewPassword);

            if (!res.Succeeded)
            {
                return BadRequest(res.Errors);
            }

            return Ok();
        }

        private async Task<(Guid?, string)> LookupUser(Guid? userId, string nick)
        {
            if (userId.HasValue && userId != Guid.Empty)
            {
                return await LookupUser(userId.Value);
            }

            if (!string.IsNullOrEmpty(nick))
            {
                return await LookupUser(nick);
            }

            return (null, null);
        }

        private async Task<(Guid?, string)> LookupUser(Guid userId)
        {
            var user = await GetUser(userId);
            if (user != null)
            {
                return (user.Id, user.UserName);
            }

            var conn = await db.UserConnections
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.Id)
                .FirstOrDefaultAsync();

            if (conn != null)
            {
                return (conn.UserId, conn.Nick);
            }

            return (null, null);
        }

        private async Task<(Guid?, string)> LookupUser(string nick)
        {
            var user = await userManager.FindByNameAsync(nick);

            if (user != null)
            {
                return (user.Id, user.UserName);
            }

            var conn = await db.UserConnections
               .Where(u => u.Nick.ToUpper() == nick.ToUpper())
               .OrderByDescending(u => u.Id)
               .FirstOrDefaultAsync();

            if (conn != null)
            {
                return (conn.UserId, conn.Nick);
            }

            return (null, null);
        }

        private async Task PublishAdminToggle(Guid id, bool up, ModerationLevel level)
        {
            await mutator.Enqueue(async (state) =>
            {
                if (!state.TryGetUser(id, out var user))
                {
                    return;
                }

                user = new StateCache.User(user.Id, user.Nick, user.IsGuest, user.Level, up);
                state.AddOrUpdateUser(user);

                var chans = state.GetUserChannelInfos(id, state.GetUserChannels(id));
                foreach (var chinfo in chans)
                {
                    var info = up ? chinfo.Up(level) : chinfo.Down();
                    state.AddOrUpdateChannelUser(info);
                    await dist.Publish(new Response<ChannelUserUpdateResponse>()
                    {
                        Subscription = chinfo.ChannelId,
                        Data = new ChannelUserUpdateResponse()
                        {
                            IsGuest = user.IsGuest,
                            UserId = user.Id,
                            Level = info.Level,
                            Nick = user.Nick
                        }
                    });
                }
            });
        }

        private static ServerBanDto FromBan(ServerBan ban)
        {
            return new ServerBanDto()
            {
                Id = ban.Id,
                BannedNick = ban.BannedNick,
                BannedUserId = ban.BannedUserId,
                Created = ban.Created,
                CreatedById = ban.CreatedById,
                CreatedByNick = ban.CreatedByNick,
                Expires = ban.Expires,
                IPFilter = ban.IPFilter,
                Reason = ban.Reason
            };
        }

        private async Task EnforceBan(ServerBan ban, ApplicationUser.UserLevel authority)
        {
            // let's kick them all
            var users = await mutator.Enqueue((state) => state.GetMatchingUsers(ban.BannedUserId, ban.IPFilter, authority));

            foreach (var u in users)
            {
                await dist.Publish(new Response<ServerBannedResponse>()
                {
                    Subscription = u.Id,
                    Data = new ServerBannedResponse()
                    {
                        Reason = ban.Reason,
                        Expires = ban.Expires,
                    }
                });
            }
        }

        protected virtual async Task<ApplicationUser> GetUser(Guid? id)
        {
            if (!id.HasValue) return null;
            return await userManager.FindByIdAsync(id.ToString()); // WTF framework
        }

        protected virtual async Task<ApplicationUser> GetUser()
        {
            return await userManager.GetUserAsync(User);
        }
    }
}
