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
    using System.Linq;
    using System.Threading.Tasks;

    [Authorize]
    public class ChannelController : ControllerBase
    {
        public const int WARNS_BEFORE_BAN = 2;

        readonly ILogger logger;
        readonly ApplicationDbContext db;
        readonly UserManager<ApplicationUser> userManager;
        readonly StateMutator mutator;
        readonly IResponsePublisher dist;

        public ChannelController(
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
        public async Task<IActionResult> GetOwnedChannels()
        {
            var user = await GetUser();

            var results = user.Channels.Select(c => FromChannel(c, user));

            return Ok(results);
        }

        [HttpGet]
        public IActionResult GetChannels(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return null;
            }
            else
            {
                var results = db.Channels
                    .Where(c => c.Name.ToLower().Contains(search.ToLower()) && !c.IsSecret)
                    .Select(c => FromDbChannel(c));

                return Ok(results);
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegisterChannel([FromBody] ChannelDto reg)
        {
            if (string.IsNullOrWhiteSpace(reg.Name))
            {
                return BadRequest("Invalid room name");
            }

            // check if they've hit max room registrations
            // j - I'm grabbing room count as a separate query. Otherwise we'll need to eager load Channels.
            var user = await GetUser();
            int roomcount = await db.Users
                .Where(u => u.Id == user.Id)
                .Select(u => u.Channels.Count)
                .FirstOrDefaultAsync();

            // Temporary for testing
            // if (roomcount >= user.MaxRooms)
            // {
            //     // TODO - better error
            //     return BadRequest("You've already registered the maximum number of rooms.");
            // }

            // check if room is already registered (name)
            var exists = await db.Channels
                .AnyAsync(c => c.Name != null && c.Name.Equals(reg.Name, StringComparison.InvariantCultureIgnoreCase));

            if (exists)
            {
                return BadRequest("That room already exists.");
            }

            var channel = new Channel()
            {
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                Owner = user,
                Name = reg.Name,
                Description = reg.Description,
                IsSecret = reg.IsSecret,
                AllowGuests = reg.AllowGuests,
                MaxUsers = 20,
            };

            db.Channels.Add(channel);

            await db.SaveChangesAsync();

            var ch = FromChannel(channel, user);

            return Ok(ch);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateChannel([FromBody] ChannelDto reg)
        {
            var user = await GetUser();

            var (channel, userLevel) = await GetModerationLevel(user, reg.Id);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + reg.Id);
            }

            if (userLevel < ModerationLevel.Admin)
            {
                return Unauthorized();
            }

            if (!channel.Name.Equals(reg.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                // check exists
                var exists = await db.Channels
                    .AnyAsync(c => c.Name.Equals(reg.Name, StringComparison.InvariantCultureIgnoreCase));

                if (exists)
                {
                    return BadRequest("Room name already in use.");
                }
            }

            // update room with options
            channel.IsSecret = reg.IsSecret;
            channel.AllowGuests = reg.AllowGuests;
            channel.Name = reg.Name;
            channel.Description = reg.Description;
            channel.LastModified = DateTime.UtcNow;
            channel.LastActivity = channel.LastModified;

            db.Channels.Update(channel);

            await db.SaveChangesAsync();

            // update the state cache
            await mutator.Enqueue(async (state) =>
            {
                var ch = new StateCache.Channel(
                    channel.Id,
                    channel.OwnerId,
                    channel.Name,
                    channel.Description,
                    channel.AllowGuests,
                    channel.IsSecret,
                    channel.MaxUsers,
                    channel.LastModified);

                state.AddOrUpdateChannel(ch);
                await dist.Publish(new Response<ChannelUpdateResponse>()
                {
                    Subscription = channel.Id,
                    Data = new ChannelUpdateResponse()
                    {
                        Name = channel.Name,
                        Description = channel.Description,
                        AllowsGuests = channel.AllowGuests,
                        IsSecret = channel.IsSecret,
                        LastModified = channel.LastModified,
                        MaxUsers = channel.MaxUsers,
                    }
                });
            });

            var results = FromChannel(channel, user);

            return Ok(results);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteChannel(Guid channelId)
        {
            var user = await GetUser();

            var (channel, level) = await GetModerationLevel(user, channelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + channelId);
            }

            if (level < ModerationLevel.RoomOwner)
            {
                return Unauthorized();
            }

            db.Channels.Remove(channel);
            await db.SaveChangesAsync();

            // TODO - how to push a 'SHUT IT ALL DOWN' message to the clients?

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> GetBans(Guid channelId)
        {
            var user = await GetUser();

            var (channel, level) = await GetModerationLevel(user, channelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + channelId);
            }

            if (level < ModerationLevel.Moderator)
            {
                return Unauthorized();
            }

            var chBans = await db.ChannelBans
                .Where(m => m.Channel.Id == channelId && m.Expires >= DateTime.UtcNow)
                .GroupJoin(
                    db.Users,
                    b => b.UserId,
                    u => u.Id,
                    (ban, users) => new { Ban = ban, User = users.FirstOrDefault() })
                .ToListAsync();

            var banDtos = chBans.Select(data =>
            {
                var us = data.User;
                var nick = (us != null) ? us.UserName : "Guest";
                var ban = data.Ban;

                return new ChannelBanDto()
                {
                    Id = ban.Id,
                    Created = ban.Created,
                    Nick = nick,
                    Level = ban.Level,
                    UserId = ban.UserId.Value,
                    ChannelId = channelId,
                    ChannelName = channel.Name,
                    CreatedBy = ban.CreatedBy,
                    Expires = ban.Expires,
                    Reason = ban.Reason
                };
            });

            return Ok(banDtos);
        }

        [HttpPost]
        public async Task<IActionResult> GetUserBans(Guid channelId, Guid userId)
        {
            var user = await GetUser();

            var (channel, level) = await GetModerationLevel(user, channelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + channelId);
            }

            if (level < ModerationLevel.Moderator)
            {
                return Unauthorized();
            }

            var bans = await db.ChannelBans
                .Where(m => m.Channel.Id == channelId && m.Expires >= DateTime.UtcNow && m.UserId == userId)
                .Join(db.Users, m => m.UserId, u => u.Id, (ban, us) => new ChannelBanDto()
                {
                    Id = ban.Id,
                    Created = ban.Created,
                    Nick = us.UserName,
                    Level = ban.Level,
                    UserId = us.Id,
                    ChannelId = channelId,
                    ChannelName = channel.Name,
                    CreatedBy = ban.CreatedBy,
                    Expires = ban.Expires,
                    Reason = ban.Reason
                })
                .ToListAsync();

            return Ok(bans);
        }

        [HttpPost]
        public async Task<IActionResult> WarnUser([FromBody] ChannelBanDto ban)
        {
            var user = await GetUser();

            var (channel, userLevel) = await GetModerationLevel(user, ban.ChannelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + ban.ChannelId);
            }

            if (userLevel < ModerationLevel.Moderator)
            {
                return Unauthorized();
            }

            var targetUser = await GetUser(ban.UserId);
            if (!await CanModerate(userLevel, targetUser, channel))
            {
                return BadRequest("Cannot moderate users with same or greater authorization level.");
            }

            // how many warnings does this user already have?
            var warnings = await db.ChannelBans
                .Where(m => m.Channel.Id == ban.ChannelId
                    && m.Expires >= DateTime.UtcNow
                    && m.UserId == ban.UserId
                    && m.Level == BanLevel.Warning)
                .ToListAsync();

            ChannelBan nban = new ChannelBan()
            {
                Channel = channel,
                UserId = ban.UserId,
                Reason = ban.Reason,
                Created = DateTime.UtcNow,
                Expires = ban.Expires,
                Level = BanLevel.Warning,
                CreatedBy = user.UserName,
            };

            if (warnings.Count < WARNS_BEFORE_BAN)
            {
                nban.Level = BanLevel.Warning;
            }
            else
            {
                nban.Level = BanLevel.Ban;

                // get addresses for the ban user
                var addys = await db.UserConnections
                    .Where(u => u.UserId == ban.UserId)
                    .Select(u => u.IPAddress)
                    .Distinct()
                    .ToListAsync();

                var banAddys = addys.Select(a => new ChannelBanAddress()
                {
                    IPFilter = a
                }).ToList();

                nban.Addresses = banAddys;

                if (string.IsNullOrEmpty(nban.Reason))
                {
                    nban.Reason = "ignoring too many warnings";
                }

                // remove the old warnings
                db.ChannelBans.RemoveRange(warnings);
            }

            db.ChannelBans.Add(nban);

            await db.SaveChangesAsync();

            var bandto = FromBan(nban);

            bandto.Nick = targetUser != null
              ? targetUser.UserName
              : await GetNickFromUserId(ban.UserId);

            await PublishBanUpdate(channel.Id, bandto, true, user);

            return Ok(FromBan(nban));
        }

        [HttpPost]
        public async Task<IActionResult> AddBan([FromBody] ChannelBanDto ban)
        {
            var user = await GetUser();

            var (channel, userLevel) = await GetModerationLevel(user, ban.ChannelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + ban.ChannelId);
            }

            if (userLevel < ModerationLevel.Moderator)
            {
                return BadRequest("You don't have access to do this.");
            }

            var targetUser = await GetUser(ban.UserId);
            if (!await CanModerate(userLevel, targetUser, channel))
            {
                return BadRequest("Cannot moderate users with same or greater authorization level.");
            }

            // get addresses for the ban user
            var addys = await db.UserConnections
                .Where(u => u.UserId == ban.UserId)
                .Select(u => u.IPAddress)
                .Distinct()
                .ToListAsync();

            var banAddys = addys.Select(a => new ChannelBanAddress()
            {
                IPFilter = a
            }).ToList();

            var nban = new ChannelBan()
            {
                Channel = channel,
                UserId = ban.UserId,
                Reason = ban.Reason,
                Created = DateTime.UtcNow,
                Expires = ban.Expires,
                Level = ban.Level,
                Addresses = banAddys,
                CreatedBy = user.UserName,
            };

            db.ChannelBans.Add(nban);

            await db.SaveChangesAsync();

            var bandto = FromBan(nban);

            bandto.Nick = targetUser != null
                ? targetUser.UserName
                : await GetNickFromUserId(ban.UserId);

            await PublishBanUpdate(channel.Id, bandto, true, user);

            return Ok(bandto);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveBan(Guid channelId, int banId)
        {
            var user = await GetUser();

            var (channel, level) = await GetModerationLevel(user, channelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + channelId);
            }

            if (level < ModerationLevel.Moderator)
            {
                return Unauthorized();
            }

            var ban = await db.ChannelBans
                .Include(b => b.Addresses)
                .FirstOrDefaultAsync(m => m.Channel.Id == channelId && m.Id == banId);

            if (ban != null)
            {
                db.ChannelBanAddresses.RemoveRange(ban.Addresses);
                db.ChannelBans.Remove(ban);
                await db.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBan([FromBody] ChannelBanDto ban)
        {
            var user = await GetUser();

            var (channel, level) = await GetModerationLevel(user, ban.ChannelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + ban.ChannelId);
            }

            if (level < ModerationLevel.Moderator)
            {
                return Unauthorized();
            }

            var fban = await db.ChannelBans
                .Where(b => b.Id == ban.Id && b.Channel.Id == ban.ChannelId)
                .FirstOrDefaultAsync();

            if (fban == null)
            {
                return NotFound("Cound not find ban with id: " + ban.Id);
            }

            fban.Reason = ban.Reason;
            fban.Expires = ban.Expires;
            fban.Level = ban.Level;
            fban.CreatedBy = user.UserName;

            db.ChannelBans.Update(fban);

            await db.SaveChangesAsync();

            var bandto = FromBan(fban);

            await PublishBanUpdate(channel.Id, bandto, false, user);

            return Ok(bandto);
        }

        [HttpPost]
        public async Task<IActionResult> GetModerators(Guid channelId)
        {
            var user = await GetUser();

            var (channel, level) = await GetModerationLevel(user, channelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + channelId);
            }

            if (level < ModerationLevel.Moderator)
            {
                return Unauthorized();
            }

            var mods = db.ChannelModerators
                .Where(m => m.Channel.Id == channelId)
                .Join(db.Users, m => m.UserId, u => u.Id, (mod, us) => new ChannelModeratorDto()
                {
                    Id = mod.Id,
                    Created = mod.Created,
                    Nick = us.UserName,
                    Level = mod.Level,
                    UserId = us.Id
                });

            return Ok(mods);
        }

        [HttpPost]
        public async Task<IActionResult> AddModerator(Guid channelId, Guid userId, ModerationLevel level)
        {
            var user = await GetUser();

            var (channel, userLevel) = await GetModerationLevel(user, channelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + channelId);
            }

            // verify moderation level
            if (userLevel < ModerationLevel.Admin)
            {
                return Unauthorized();
            }

            var targetUser = await GetUser(userId);
            if (targetUser == null)
            {
                return NotFound("Cannot find user with id: " + userId);
            }

            if (!await CanModerate(userLevel, targetUser, channel))
            {
                return BadRequest("Cannot moderate users with same or greater authorization level.");
            }

            // check if already a mod of some sort.
            // if so, delete that one and recreate
            var mod = await db.ChannelModerators
                .Where(m => m.ChannelId == channelId && m.UserId == userId)
                .FirstOrDefaultAsync();

            if (mod != null)
            {
                db.ChannelModerators.Remove(mod);
            }

            var nmod = new ChannelModerator()
            {
                User = targetUser,
                Channel = channel,
                Level = level,
            };

            db.ChannelModerators.Add(nmod);

            await db.SaveChangesAsync();

            var dto = FromModerator(nmod);

            await PublishModeratorUpdate(channel.Id, dto);

            return Ok(dto);

        }

        [HttpPost]
        public async Task<IActionResult> RemoveModerator(Guid channelId, Guid userId)
        {
            var user = await GetUser();

            var (channel, level) = await GetModerationLevel(user, channelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + channelId);
            }

            if (level < ModerationLevel.Admin)
            {
                return Unauthorized();
            }

            var mod = await db.ChannelModerators
               .Include(m => m.User)
               .Where(m => m.ChannelId == channelId && m.UserId == userId)
               .FirstOrDefaultAsync();

            if (mod == null)
            {
                NotFound("Could not find moderator.");
            }

            if (!await CanModerate(level, mod.User, channel))
            {
                return BadRequest("Cannot moderate users with same or greater authorization level.");
            }

            db.ChannelModerators.Remove(mod);

            await db.SaveChangesAsync();

            var dto = FromModerator(mod);
            dto.Level = ModerationLevel.Normal;
            await PublishModeratorUpdate(channelId, dto);

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SetModeratorLevel(Guid channelId, Guid userId, ModerationLevel level)
        {
            var user = await GetUser();

            var (channel, userLevel) = await GetModerationLevel(user, channelId);

            if (channel == null)
            {
                return NotFound("Cound not find channel with id: " + channelId);
            }

            if (userLevel < ModerationLevel.Admin)
            {
                return Unauthorized();
            }

            var targetUser = await GetUser(userId);
            if (targetUser == null)
            {
                return NotFound("Cannot find user with id: " + userId);
            }

            if (!await CanModerate(userLevel, targetUser, channel))
            {
                return BadRequest("Cannot moderate users with same or greater authorization level.");
            }

            var mod = await db.ChannelModerators
               .Include(m => m.User)
               .Where(m => m.ChannelId == channelId && m.UserId == userId)
               .FirstOrDefaultAsync();

            if (mod == null)
            {
                return NotFound("Could not find moderator.");
            }

            mod.Level = level;

            db.Update(mod);

            await db.SaveChangesAsync();

            var dto = FromModerator(mod);

            await PublishModeratorUpdate(channel.Id, dto);

            return Ok(dto);
        }

        private bool CanModerate(ModerationLevel level, ModerationLevel targetLevel)
        {
            return (level >= ModerationLevel.ServerAdmin || targetLevel < level);
        }

        private async Task<bool> CanModerate(ModerationLevel level, ApplicationUser targetUser, Channel channel)
        {
            if (targetUser == null) return true;

            var targetLevel = await GetModerationLevel(targetUser, channel);

            return CanModerate(level, targetLevel);
        }

        private async Task<ModerationLevel> GetModerationLevel(ApplicationUser user, Channel ch)
        {
            if (user.Level >= ApplicationUser.UserLevel.Admin)
            {
                return ModerationLevel.ServerAdmin;
            }

            if (ch.OwnerId == user.Id)
            {
                return ModerationLevel.RoomOwner;
            }

            var mod = await db.ChannelModerators
                .Where(m => m.ChannelId == ch.Id && m.UserId == user.Id)
                .FirstOrDefaultAsync();

            if (mod == null)
            {
                return ModerationLevel.Unauthorized;
            }

            return mod.Level;
        }

        private async Task<(Channel, ModerationLevel)> GetModerationLevel(ApplicationUser user, Guid channelId)
        {
            var ch = await db.Channels.FirstOrDefaultAsync(c => c.Id == channelId);

            if (ch == null)
            {
                return (null, ModerationLevel.Unauthorized);
            }

            var level = await GetModerationLevel(user, ch);

            return (ch, level);
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

        private async Task PublishModeratorUpdate(Guid channelId, ChannelModeratorDto mod)
        {
            // update the state cache
            await mutator.Enqueue(async (state) =>
            {
                if (state.TryGetChannelUserInfo(channelId, mod.UserId, out var info)
                    && state.TryGetUser(mod.UserId, out var user))
                {
                    var newLevel = mod.Level > info.Level ? mod.Level : info.Level;
                    state.AddOrUpdateChannelUser(new StateCache.ChannelUserInfo(mod.UserId, channelId, newLevel, mod.Level));
                    await dist.Publish(new Response<ChannelUserUpdateResponse>()
                    {
                        Subscription = channelId,
                        Data = new ChannelUserUpdateResponse()
                        {
                            UserId = mod.UserId,
                            Nick = user.Nick,
                            IsGuest = user.IsGuest,
                            Level = mod.Level
                        }
                    });
                }
            });
        }

        private async Task PublishBanUpdate(Guid channelId, ChannelBanDto ban, bool notify, ApplicationUser notifiySender)
        {
            if (notify)
            {
                var timestamp = DateTime.UtcNow;
                var timestampms = ((DateTimeOffset)timestamp).ToUnixTimeMilliseconds();

                var type = ban.Level == BanLevel.Ban ? "banned" : "warned";
                var msg = $"{ban.Nick} has been {type} for {ban.Reason}";

                // publish the ban to the room
                var resp = new Response<ChannelMessageResponse>()
                {
                    Subscription = channelId,
                    Timestamp = timestampms,
                    Data = new ChannelMessageResponse()
                    {
                        UserId = notifiySender.Id,
                        Message = msg,
                        Type = MessageTypes.NOTIFICATION,
                    }
                };

                resp.Id = await db.SavePost(
                    resp.Subscription,
                    resp.Data.UserId,
                    timestamp,
                    resp.Data.Message,
                    notifiySender.UserName,
                    resp.Data.Type);

                await dist.Publish(resp);
            }

            // update the state cache
            await mutator.Enqueue(async (state) =>
            {
                if (state.TryGetChannelUserInfo(channelId, ban.UserId, out var info)
                    && state.TryGetUser(ban.UserId, out var user))
                {
                    await dist.Publish(new Response<ChannelBannedResponse>()
                    {
                        Subscription = channelId,
                        Data = new ChannelBannedResponse()
                        {
                            UserId = ban.UserId,
                            Expires = ban.Expires,
                            Level = ban.Level,
                            Reason = ban.Reason,
                            ChannelName = ban.ChannelName,
                            ModeratorNick = ban.CreatedBy
                        }
                    });

                    if (ban.Level == BanLevel.Ban && info.Level < ModerationLevel.Moderator)
                    {
                        // remove the user from the channel
                        state.RemoveChannelUser(ban.ChannelId, ban.UserId);

                        await dist.Publish(new Response<ChannelPartResponse>()
                        {
                            Subscription = channelId,
                            Data = new ChannelPartResponse()
                            {
                                UserId = ban.UserId,
                            }
                        });
                    }
                }
            });
        }

        public static ChannelBanDto FromBan(ChannelBan ban)
        {
            return new ChannelBanDto()
            {
                ChannelId = ban.Channel.Id,
                ChannelName = ban.Channel.Name,
                CreatedBy = ban.CreatedBy,
                UserId = ban.UserId.Value,
                Reason = ban.Reason,
                Created = DateTime.UtcNow,
                Expires = ban.Expires,
                Level = ban.Level,
            };
        }

        public static ChannelModeratorDto FromModerator(ChannelModerator mod)
        {
            return new ChannelModeratorDto()
            {
                Id = mod.Id,
                Created = mod.Created,
                Nick = mod.User.UserName,
                Level = mod.Level,
                UserId = mod.UserId
            };
        }

        public static ChannelDto FromChannel(Channel ch, ApplicationUser us)
        {
            return new ChannelDto()
            {
                Id = ch.Id,
                Name = ch.Name,
                Description = ch.Description,
                AllowGuests = ch.AllowGuests,
                MaxUsers = ch.MaxUsers,
                IsSecret = ch.IsSecret,
                Created = ch.Created,
                LastActivity = ch.LastActivity,
                LastModified = ch.LastModified,
                OwnerNick = us?.UserName,
                OwnerId = ch.OwnerId,
            };
        }

        public static ListResponse.ListChannel FromDbChannel(Channel ch)
        {
            return new ListResponse.ListChannel()
            {
                Id = ch.Id,
                Name = ch.Name,
                Description = ch.Description,
                AllowsGuests = ch.AllowGuests,
                IsSecret = ch.IsSecret,
                MaxUsers = ch.MaxUsers,
                UserCount = 0
            };
        }

        public async Task<string> GetNickFromUserId(Guid userId)
        {
            // yanks it out of chat history
            return await db.UserConnections
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Id)
                .Select(c => c.Nick)
                .FirstOrDefaultAsync();
        }
    }
}