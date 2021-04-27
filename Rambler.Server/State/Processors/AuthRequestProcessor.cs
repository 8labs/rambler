namespace Rambler.Server.State.Processors
{
    using Contracts.Requests;
    using Contracts.Responses;
    using Contracts.Server;
    using Database;
    using Database.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Socket;
    using State;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    public class AuthRequestProcessor : IRequestProcessor<AuthRequest>
    {
        private readonly StateMutator mutator;
        private readonly IResponsePublisher dist;
        private readonly ILogger log;
        private readonly IAuthorize authorize;
        private readonly ApplicationDbContext db;

        /// <summary>
        /// Validates a name against a regex
        /// Should move this somewhere at somepoint....
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            const string pattern = @"^([\w-_]{1,15})$";

            var regex = new System.Text.RegularExpressions.Regex(pattern);
            return regex.IsMatch(name);
        }

        public AuthRequestProcessor(
            StateMutator mutator,
            ApplicationDbContext db,
            IResponsePublisher dist,
            IAuthorize authorize,
            ILogger<AuthRequestProcessor> log)
        {
            this.mutator = mutator;
            this.dist = dist;
            this.authorize = authorize;
            this.log = log;
            this.db = db;
        }

        public async Task<IEnumerable<ServerBan>> GetServerBans(Guid userId, string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                log.LogWarning("No IP from proxy");
                return Enumerable.Empty<ServerBan>();
            }

            //  EF.Functions.Like(s.Title, "%angel%")
            var bans = await db.ServerBans
                .Where(m => (m.BannedUserId == userId || m.IPFilter == ip) && m.Expires >= DateTime.UtcNow)
                .ToListAsync();

            return bans;
        }

        public async Task Process(Request<AuthRequest> req)
        {
            // we're double checking the auth token here
            // this validates the nick is correct and the client isn't spoofing
            // might also catch any additional weirdness.
            var id = authorize.Authorize(req.Data.Token, true);

            if (id == null)
            {
                await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NotAuthenticated);
                return;
            }

            // TODO - this should have happened in the webservice already
            if (!IsValidName(id.Nick))
            {
                await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.InvalidName);
                return;
            }

            // admin tokens we verify against the DB, just in case.
            if (id.Level > 0)
            {
                var user = await db.Users.FindAsync(id.UserId);
                if (user == null || (int)user.Level != id.Level)
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NotAuthenticated);
                    return;
                }

            }

            // only check server bans on non-admins
            if (id.Level < (int)ApplicationUser.UserLevel.Admin)
            {
                // do a check against server bans
                // TODO:  should this be cached?  (It'd be nice if this was at the socket layer...)
                // might need server level subs at some point to pass this kinda update to the sockets
                var bans = await GetServerBans(id.UserId, req.IPAddress);
                if (bans.Any())
                {
                    var b = bans.First();
                    await dist.Publish(new Response<ServerBannedResponse>()
                    {
                        Subscription = req.SocketId,
                        Data = new ServerBannedResponse()
                        {
                            Expires = b.Expires,
                            Reason = b.Reason,
                        }
                    });
                    return;
                }
            }

            await mutator.Enqueue(async (state) =>
            {
                // check that the nick isn't in use by a different user
                // TODO: this is odd here.  It's basically here because guests.
                if (state.TryGetUser(id.Nick, out var user) && (user.Id != id.UserId))
                {
                    await dist.PublishError(req.SocketId, ErrorResponse.ErrorCode.NickInUse);
                    return;
                }

                state.AddOrUpdateSocket(new StateCache.Socket(req.SocketId, req.UserId, req.IPAddress));
                state.AddOrUpdateUser(new StateCache.User(req.UserId, id.Nick, id.IsGuest, id.Level, true));

                var count = state.GetUserSocketCount(req.UserId);

                // all the information needed by the client to figure itself out
                await dist.Publish(new Response<AuthResponse>()
                {
                    Subscription = req.SocketId,
                    Data = new AuthResponse()
                    {
                        Nick = id.Nick,
                        UserId = req.UserId,
                        SocketId = req.SocketId,
                        ConnectionCount = count,
                    }
                });
            });

            await mutator.Enqueue(async (state) =>
            {
                // if user is already in channels (existing connection)
                // resend all the channels 
                var channels = state.GetUserChannels(req.UserId);
                foreach (var chid in channels)
                {
                    // Note: This works different than the normal join
                    // it only sends to the socket instead of broadcasting to all users
                    // because of this, UserId is part of the sending data.
                    // TODO: This may be worth another message type...
                    state.TryGetChannel(chid, out var chan);
                    state.TryGetChannelUserInfo(chid, req.UserId, out var info);

                    await dist.Publish(new Response<JoinResponse>()
                    {
                        Subscription = req.SocketId,
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

                    var chUsers = state.GetRoomUsers(chid);
                    await dist.Publish(new Response<ChannelUsersResponse>()
                    {
                        Subscription = req.SocketId,
                        Data = new ChannelUsersResponse()
                        {
                            ChannelId = chid,
                            Users = chUsers,
                        }
                    });
                }
            });


            db.UserConnections.Add(new UserConnection()
            {
                ConnectedOn = DateTime.UtcNow,
                IPAddress = req.IPAddress,
                UserId = req.UserId,
                IsGuest = id.IsGuest,
                Nick = id.Nick,
            });
            await db.SaveChangesAsync();
        }
    }
}
