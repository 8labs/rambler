namespace Rambler.Server.State
{
    using Contracts.Responses;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Utility;
    using Database.Models;
    using Contracts.Api;

    /// <summary>
    /// Current state of the cluster
    /// Tracks online users and users in channels
    /// Quick in-memory hack - the whole thing will need replaced
    /// Note: not thread safe.  thread locking should be handled elsewhere.
    /// 
    /// Cached classes are immutable.  
    /// This allows us to pass them around to other threads w/out worrying about it.
    /// </summary>
    public class StateCache
    {
        public class User
        {
            public Guid Id { get; }
            public string Nick { get; }
            public bool IsGuest { get; }
            public bool IsUp { get; }
            public int Level { get; }

            public User(Guid id, string nick, bool isGuest, int level, bool isUp)
            {
                Id = id;
                Nick = nick;
                IsGuest = isGuest;
                Level = level;
                IsUp = isUp;
            }
        }

        public class ChannelUserInfo
        {
            public Guid UserId { get; }
            public Guid ChannelId { get; }

            /// <summary>
            /// current level in room
            /// </summary>
            public ModerationLevel Level { get; }

            /// <summary>
            /// Admins can 'down' so this is their 'normal' level when they switch back to down
            /// </summary>
            public ModerationLevel BaseLevel { get; }

            public ChannelUserInfo(Guid userId, Guid channelId, ModerationLevel level, ModerationLevel baseLevel)
            {
                UserId = userId;
                ChannelId = channelId;
                Level = level;
                BaseLevel = baseLevel;
            }

            public ChannelUserInfo Up(ModerationLevel level)
            {
                return new ChannelUserInfo(UserId, ChannelId, level, BaseLevel);
            }

            public ChannelUserInfo Down()
            {
                return new ChannelUserInfo(UserId, ChannelId, BaseLevel, BaseLevel);
            }
        }

        public class Channel
        {
            public Guid Id { get; }
            public Guid OwnerId { get; }
            public string Name { get; }
            public string Description { get; }
            public bool AllowsGuests { get; }
            public bool IsSecret { get; }
            public int MaxUsers { get; }
            public DateTime LastModified { get; }

            public Channel(Guid id, Guid ownerId, string name, string description, bool allowsGuests, bool isSecret, int maxUsers, DateTime modified)
            {
                Id = id;
                OwnerId = ownerId;
                Name = name;
                Description = description;
                AllowsGuests = allowsGuests;
                IsSecret = isSecret;
                MaxUsers = maxUsers;
                LastModified = modified;
            }
        }

        public class Socket
        {
            public Guid Id { get; }
            public Guid UserId { get; }
            public string IPAddress { get; }

            public Socket(Guid id, Guid userId, string ip)
            {
                Id = id;
                UserId = userId;
                IPAddress = ip;
            }
        }

        private readonly UniqueIndex<Guid, Socket> Sockets = new UniqueIndex<Guid, Socket>();
        private readonly UniqueIndex<Guid, User> Users = new UniqueIndex<Guid, User>();
        private readonly UniqueIndex<Guid, Channel> Channels = new UniqueIndex<Guid, Channel>();
        private readonly UniqueIndex<(Guid, Guid), ChannelUserInfo> ChannelUserInfos = new UniqueIndex<(Guid, Guid), ChannelUserInfo>();

        private readonly UniqueIndex<string, Guid> UserNicks = new UniqueIndex<string, Guid>(StringComparer.OrdinalIgnoreCase);
        private readonly Index<Guid, Guid> UserChannels = new Index<Guid, Guid>();
        private readonly Index<Guid, Guid> UserSockets = new Index<Guid, Guid>();

        private readonly UniqueIndex<string, Guid> ChannelNames = new UniqueIndex<string, Guid>(StringComparer.OrdinalIgnoreCase);
        private readonly Index<Guid, Guid> ChannelUsers = new Index<Guid, Guid>();

        public void AddOrUpdateUser(User user)
        {
            if (Users.TryGet(user.Id, out var u))
            {
                UserNicks.Remove(u.Nick);
            }

            Users.Set(user.Id, user);
            UserNicks.Add(user.Nick, user.Id);
        }

        public void RemoveUser(Guid userId)
        {
            if (!Users.TryGet(userId, out var user)) { return; }

            foreach (var chId in UserChannels.Get(user.Id))
            {
                ChannelUsers.Remove(chId, user.Id);
                ChannelUserInfos.Remove((chId, user.Id));
            }

            UserChannels.Remove(user.Id);
            UserNicks.Remove(user.Nick);
            UserSockets.Remove(user.Id);
            Users.Remove(user.Id);
        }

        public void AddOrUpdateSocket(Socket socket)
        {
            if (Sockets.TryGet(socket.Id, out var old))
            {
                UserSockets.Remove(socket.UserId, socket.Id);
            }

            Sockets.Set(socket.Id, socket);
            UserSockets.Add(socket.UserId, socket.Id);
        }

        public void RemoveSocket(Guid socketId)
        {
            if (!Sockets.TryGet(socketId, out var socket)) { return; }

            Sockets.Remove(socketId);
            UserSockets.Remove(socket.UserId, socketId);
        }

        public int GetUserSocketCount(Guid userId)
        {
            return UserSockets.GetCount(userId);
        }

        public IEnumerable<Socket> GetUserSockets(Guid userId)
        {
            foreach (var id in UserSockets.Get(userId))
            {
                if (Sockets.TryGet(id, out var s))
                {
                    yield return s;
                }
            }
        }

        public int GetServerUserCount()
        {
            return Users.Count;
        }

        public int GetRoomUserCountByName(string name)
        {
            if (ChannelNames.TryGet(name, out var chid))
            {
                return ChannelUsers.GetCount(chid);
            }
            return 0;
        }

        public void AddOrUpdateChannel(Channel channel)
        {
            if (Channels.TryGet(channel.Id, out var old))
            {
                ChannelNames.Remove(old.Name);
            }

            Channels.Set(channel.Id, channel);
            ChannelNames.Add(channel.Name, channel.Id);
        }

        public void RemoveChannel(Guid chanId)
        {
            if (!Channels.TryGet(chanId, out var ch)) { return; }

            foreach (var u in ChannelUsers.Get(ch.Id))
            {
                UserChannels.Remove(u, ch.Id);
                ChannelUserInfos.Remove((ch.Id, u));
            }

            ChannelUsers.Remove(ch.Id);
            ChannelNames.Remove(ch.Name);
            Channels.Remove(ch.Id);
        }

        public bool AddOrUpdateChannelUser(ChannelUserInfo info)
        {
            if (!Channels.TryGet(info.ChannelId, out var ch)) return false;
            if (!Users.TryGet(info.UserId, out var user)) return false;

            var id = (info.ChannelId, info.UserId);
            if (!ChannelUserInfos.TryGet(id, out var old))
            {
                UserChannels.Add(info.UserId, info.ChannelId);
                ChannelUsers.Add(info.ChannelId, info.UserId);
            }

            ChannelUserInfos.Set(id, info);

            return true;
        }

        public bool TryGetChannelUserInfo(Guid chanId, Guid userId, out ChannelUserInfo info)
        {
            return ChannelUserInfos.TryGet((chanId, userId), out info);
        }

        public bool RemoveChannelUser(Guid chanId, Guid userId)
        {
            var id = (chanId, userId);
            if (!ChannelUserInfos.TryGet(id, out var info)) { return false; }

            ChannelUsers.Remove(info.ChannelId, info.UserId);
            UserChannels.Remove(info.UserId, info.ChannelId);
            ChannelUserInfos.Remove(id);

            return true;
        }

        public bool IsUserInChannel(Guid chanId, Guid userId)
        {
            return UserChannels.Has(userId, chanId);
        }

        public bool TryGetChannel(Guid chanId, out Channel channel)
        {
            return Channels.TryGet(chanId, out channel);
        }

        public bool TryGetChannel(string name, out Channel channel)
        {
            if (ChannelNames.TryGet(name, out var id))
            {
                return TryGetChannel(id, out channel);
            }

            channel = null;
            return false;
        }

        public bool TryGetUser(Guid userId, out User user)
        {
            return Users.TryGet(userId, out user);
        }

        public bool TryGetUser(string nick, out User user)
        {
            if (UserNicks.TryGet(nick, out var id))
            {
                return TryGetUser(id, out user);
            }

            user = null;
            return false;
        }

        public IEnumerable<Channel> GetChannels(IEnumerable<Guid> channelIds)
        {
            foreach (var id in channelIds)
            {
                if (Channels.TryGet(id, out var ch))
                {
                    yield return ch;
                }
            }
        }

        public IEnumerable<ChannelUserInfo> GetUserChannelInfos(Guid userId, IEnumerable<Guid> channelIds)
        {
            foreach (var id in channelIds)
            {
                if (ChannelUserInfos.TryGet((id, userId), out var info))
                {
                    yield return info;
                }
            }
        }

        public IEnumerable<ChannelUserInfo> GetChannelUserInfos(Guid channelId, IEnumerable<Guid> userIds)
        {
            foreach (var id in userIds)
            {
                if (ChannelUserInfos.TryGet((channelId, id), out var info))
                {
                    yield return info;
                }
            }
        }

        public IEnumerable<Guid> GetUserChannels(Guid userId)
        {
            return UserChannels.Get(userId);
        }

        public IEnumerable<Guid> GetChannelUsers(Guid chanId)
        {
            return ChannelUsers.Get(chanId);
        }

        public int GetChannelUserCount(Guid chanId)
        {
            return ChannelUsers.GetCount(chanId);
        }

        public bool HasChannel(string chanName)
        {
            return ChannelNames.Has(chanName);
        }

        public bool HasChannel(Guid chanId)
        {
            return Channels.Has(chanId);
        }

        public bool HasUser(string nick)
        {
            return UserNicks.Has(nick);
        }

        public bool HasUser(Guid userId)
        {
            return Users.Has(userId);
        }

        public IEnumerable<User> GetMatchingUsers(Guid? userId, string ipfilter, ApplicationUser.UserLevel authority)
        {
            var socks = Sockets.Values
                .Where(s => (userId.HasValue && s.UserId == userId.Value) || (!string.IsNullOrWhiteSpace(ipfilter) && s.IPAddress == ipfilter));

            var users = socks.Select(s =>
            {
                if (TryGetUser(s.UserId, out var su))
                {
                    return su;
                }
                return null;
            })
            .Where(su => su != null && su.Level < (int)authority)
            .Distinct()
            .ToList();

            return users;
        }

        public IEnumerable<ListResponse.ListChannel> GetPublicChannels()
        {
            return Channels
                .Values
                .Where(c => !c.IsSecret)
                .Select(FromChannel)
                .Where(c => c.UserCount > 0)
                .ToList();
        }

        public IEnumerable<ListResponse.ListChannel> GetPublicChannels(Func<string, bool> search)
        {
            return Channels
                .Values
                .Where(c => !c.IsSecret && search(c.Name))
                .Select(FromChannel)
                .Where(c => c.UserCount > 0)
                .ToList();
        }

        public List<ChannelUsersResponse.RoomUser> GetRoomUsers(Guid channelId)
        {
            var users = GetChannelUsers(channelId);
            var userNicks = users.Select(u =>
            {
                TryGetUser(u, out var user);
                TryGetChannelUserInfo(channelId, u, out var info);
                return new ChannelUsersResponse.RoomUser()
                {
                    Id = u,
                    Nick = user.Nick,
                    IsGuest = user.IsGuest,
                    ModLevel = (int)info.Level,
                };
            });

            return userNicks.ToList();
        }

        public List<ServerUserInfoDto.UserChannelInfo> GetUserChannelInfos(Guid userId)
        {
            return GetUserChannels(userId)
                .Select(chId =>
                {
                    TryGetChannelUserInfo(chId, userId, out var info);
                    TryGetChannel(chId, out var ch);
                    return new ServerUserInfoDto.UserChannelInfo()
                    {
                        ChannelId = chId,
                        Name = ch.Name,
                        Level = info.Level,
                    };
                })
                .ToList();
        }

        private ListResponse.ListChannel FromChannel(Channel c)
        {
            return new ListResponse.ListChannel()
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                AllowsGuests = c.AllowsGuests,
                IsSecret = c.IsSecret,
                MaxUsers = c.MaxUsers,
                UserCount = GetChannelUserCount(c.Id)
            };
        }
    }
}
