namespace Rambler.Server.State.Processors
{
    using Contracts.Api;
    using Contracts.Requests;
    using Contracts.Responses;
    using Contracts.Server;
    using Database;
    using Database.Models;
    using Microsoft.EntityFrameworkCore;
    using State;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class JoinRequestProcessor : IRequestProcessor<JoinRequest>
    {
        private StateMutator mutator;
        private IResponsePublisher dist;
        private ApplicationDbContext db;
        private ILogger<JoinRequestProcessor> log;

        public JoinRequestProcessor(StateMutator mutator, IResponsePublisher dist, ApplicationDbContext db, ILogger<JoinRequestProcessor> log)
        {

            this.mutator = mutator;
            this.dist = dist;
            this.db = db;
            this.log = log;
        }

        public async Task<IEnumerable<ChannelBan>> GetChannelBans(Guid chanId, Guid userId, string ip)
        {
            var address = ip.ToString();
            var bans = await db.ChannelBans
                .Include(m => m.Addresses)
                .Include(m => m.Channel)
                .Where(m => m.Channel.Id == chanId && m.Expires >= DateTime.UtcNow)
                .Where(m => m.Addresses.Any(a => a.IPFilter.StartsWith(ip.ToString())))
                .ToListAsync();

            return bans;
        }

        public async Task<StateCache.ChannelUserInfo> GetUserInfo(StateCache.Channel ch, StateCache.User user)
        {
            if (ch.OwnerId == user.Id)
            {
                return new StateCache.ChannelUserInfo(user.Id, ch.Id, ModerationLevel.RoomOwner, ModerationLevel.RoomOwner);
            }

            var mod = await db.ChannelModerators
                .FirstOrDefaultAsync(m => m.Channel.Id == ch.Id && m.User.Id == user.Id);

            if (mod != null)
            {
                return new StateCache.ChannelUserInfo(user.Id, ch.Id, mod.Level, mod.Level);
            }
            else
            {
                return new StateCache.ChannelUserInfo(user.Id, ch.Id, ModerationLevel.Normal, ModerationLevel.Normal);
            }
        }

        public async Task<StateCache.Channel> LoadChannel(Guid? id, string name)
        {
            // first, try loading it from the state
            var channel = await mutator.Enqueue((state) =>
            {
                StateCache.Channel ch = null;
                if (id.HasValue)
                {
                    state.TryGetChannel(id.Value, out ch);
                }
                else if (!string.IsNullOrWhiteSpace(name))
                {
                    state.TryGetChannel(name, out ch);
                }

                return ch;
            });

            if (channel != null) { return channel; }

            //next, try loading it from the db
            var room = !string.IsNullOrWhiteSpace(name)
                ? await db.Channels.SingleOrDefaultAsync(c => c.Name == name)
                : await db.Channels.SingleOrDefaultAsync(c => c.Id == id);

            if (room == null) { return null; }

            return await mutator.Enqueue((state) =>
            {
                // there's a chance for a race condition here
                // make sure another join hasn't already added it
                if (state.TryGetChannel(room.Id, out var existing)
                    && existing.LastModified > room.LastModified)
                {
                    return null;
                }

                var ch = new StateCache.Channel(
                    room.Id,
                    room.OwnerId,
                    room.Name,
                    room.Description,
                    room.AllowGuests,
                    room.IsSecret,
                    room.MaxUsers,
                    room.LastModified);

                state.AddOrUpdateChannel(ch);

                return ch;
            });
        }

        public async Task Process(Request<JoinRequest> req)
        {
            var stateUser = await mutator.Enqueue((state) =>
            {
                state.TryGetUser(req.UserId, out var user);
                return user;
            });

            if (stateUser == null)
            {
                await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NotAuthenticated);
                return;
            }

            var exChan = await LoadChannel(req.Data.ChannelId, req.Data.ChannelName);

            if (exChan == null)
            {
                await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NoSuchChannel);
                return;
            }

            // TODO - check for validated emails too
            if (stateUser.IsGuest && !exChan.AllowsGuests)
            {
                await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NoGuestsAllowed);
                return;
            }

            var info = await GetUserInfo(exChan, stateUser);
            if (stateUser.IsUp && stateUser.Level >= (int)ApplicationUser.UserLevel.Admin)
            {
                // admin is up, make sure they join as up
                info = info.Up((ModerationLevel)stateUser.Level);
            }

            var allbans = await GetChannelBans(exChan.Id, req.UserId, req.IPAddress);
            var mutes = allbans.Where(b => b.Level == BanLevel.Mute);
            var warnings = allbans.Where(b => b.Level == BanLevel.Warning);
            var bans = allbans.Where(b => b.Level == BanLevel.Ban);

            if (info.Level <= ModerationLevel.Normal && bans.Any())
            {
                // just send out a single ban message for now
                var b = bans.First();
                await dist.Publish(new Response<ChannelBannedResponse>()
                {
                    Subscription = req.SocketId,
                    Data = new ChannelBannedResponse()
                    {
                        Expires = b.Expires,
                        Reason = b.Reason,
                        UserId = req.UserId,
                        ChannelName = b.Channel.Name,
                        ModeratorNick = b.CreatedBy
                    }
                });

                return;
            }

            await mutator.Enqueue(async (state) =>
            {
                // double check their state hasn't changed since queries happened
                if (!state.TryGetUser(req.UserId, out var user) || (stateUser.Level != user.Level))
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NotAuthenticated);
                    return;
                }

                if (!state.TryGetChannel(exChan.Id, out var chan))
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NoSuchChannel);
                    return;
                }

                if (state.IsUserInChannel(chan.Id, req.UserId))
                {
                    //await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.AlreadyInChannel);
                    return;
                }

                state.AddOrUpdateChannelUser(info);

                await dist.Publish(new Response<JoinResponse>()
                {
                    Subscription = req.UserId,
                    Data = new JoinResponse()
                    {
                        ChannelId = chan.Id,
                        Name = chan.Name,
                        Description = chan.Description,
                        MaxUsers = chan.MaxUsers,
                        IsSecret = chan.IsSecret,
                        AllowsGuests = chan.AllowsGuests,
                        UserId = req.UserId,
                        Level = info.Level,
                    }
                });

                var chUsers = state.GetRoomUsers(chan.Id);
                await dist.Publish(new Response<ChannelUsersResponse>()
                {
                    Subscription = req.SocketId,
                    Data = new ChannelUsersResponse()
                    {
                        ChannelId = chan.Id,
                        Users = chUsers,
                    }
                });

                //prep the message to channel users
                await dist.Publish(new Response<ChannelJoinedResponse>()
                {
                    Subscription = chan.Id,
                    Data = new ChannelJoinedResponse()
                    {
                        UserId = req.UserId,
                        Nick = user.Nick,
                        IsGuest = user.IsGuest,
                        Level = info.Level,
                    },
                });

                if (warnings.Any())
                {
                    // handle any warning messages after the state joins are all completed
                    await dist.Publish(new Response<ChannelWarnedResponse>()
                    {
                        Subscription = req.SocketId,
                        Data = new ChannelWarnedResponse()
                        {
                            Warnings = warnings.Select(w => new ChannelWarnedResponse.Warning()
                            {
                                Expires = w.Expires,
                                Reason = w.Reason,
                                UserId = req.UserId
                            }).ToList()
                        }
                    });
                }
            });
        }
    }
}
