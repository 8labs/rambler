namespace Rambler {

  export interface IHash<TValue> {
    [key: string]: TValue;
  }

  export interface IUser {
    id: System.Guid;
    isSelf: boolean;
    isGuest: boolean;
    nick: string;
    avatar: string;
  }

  export interface IChannelUser {
    id: System.Guid;
    modLevel: Rambler.Contracts.Api.ModerationLevel;
  }

  export interface IChannel {
    id: System.Guid;
    userId: System.Guid; // userId for dms
    name: string;
    description: string;
    maxUsers: number;
    isSecret: boolean;
    last: number;
    unread: number;
    bans: ChannelBanDto[];
    notifications: any[];
    isRoom: boolean;
    allowsGuests: boolean;
    lastModified: Date;
    isSynced: boolean;
    isSyncing: boolean;
    newMsg: string;
  }

  export interface IChannelResponse {
    UserId: string;
  }

  export interface IChatMessage extends IResponse {
    // Denormalized info used to display messages
    // isSelf: boolean;
    // nick: string;
    // avatar: string;
  }

  export class ChatState {

    public MAX_MESSAGES: number = 200;

    public userId: System.Guid;

    public identity: IdentityToken | undefined;

    public channel: IChannel | undefined;

    public channels: IChannel[] = [];

    public channelMessages: IHash<IChatMessage[]> = {};

    public channelUsers: IHash<IChannelUser[]> = {};

    public users: IHash<IUser> = {};

    public userNickLookup: IHash<string> = {};

    public avatars: IEmoji[];

    public ignoreLookup: IHash<IgnoreDto> = {};

    // random crap
    public ban: ChannelBannedResponse;
    public error_code: number;

    constructor(private store: LocalStoreService) {

    }

    public parseToken(token: string) {
      const pieces = token.split('.');
      if (pieces.length != 3) return;
      try {
        const data = atob(pieces[1]);
        this.identity = JSON.parse(data);
      } catch (er) {
        console.log('error parsing token');
      }
    }

    public setToken(token: string) {
      this.parseToken(token);
      this.store.saveToken(token);
    }

    public getToken() {
      return this.store.getToken();
    }

    public clearToken() {
      this.identity = undefined;
      this.store.removeToken();
    }

    public getUser(id: string): IUser | undefined {
      return this.users[id];
    }

    public userInRoom(id: string): boolean {
      if (this.getUser(id) && this.channel && this.channel.isRoom) {
        let users = this.getChannelUsers(this.channel.id);
        if (users) {
          return !!users.find((u) => {
            return u.id === id;
          });
        }
      }
      else if (this.getUser(id) && (!this.channel || !this.channel.isRoom)) {
        return true;
      }

      return false;
    }

    public getUserByNick(nick: string): IUser | undefined {
      for (let k of Object.keys(this.userNickLookup)) {
        if (k.toUpperCase() == nick.toUpperCase()) {
          const uid = this.userNickLookup[k];
          return this.users[uid];
        }
      }
      return undefined;
    }

    public addUser(user: IUser, overwrite: boolean = false) {
      var u = this.users[user.id];
      if (u && !overwrite) {
        return;
      }
      this.users[user.id] = user;
      this.userNickLookup[user.nick] = user.id;
      this.store.saveUser(user);
    }

    public removeUser(id: string) {
      delete this.users[id];
      this.store.removeUser(id);
    }

    public updateUser(id: string, update: (user: IUser) => void, create: boolean = false) {
      let u = this.getUser(id);
      if (!u && !create) return undefined;
      if (!u) {
        u = {
          id: id,
          isSelf: id == this.userId,
          isGuest: false,
          nick: '',
          avatar: '',
        }
      }
      update(u);
      this.users[u.id] = u;
      this.userNickLookup[u.nick] = u.id;
      this.store.saveUser(u);
      return u;
    }

    public getChannelMessages(id: string) {
      return this.channelMessages[id];
    }

    public getChannelUsers(id: string) {
      return this.channelUsers[id];
    }

    public getChannelUser(ch: IChannel, uId: string): IChannelUser | undefined {
      const users = this.channelUsers[ch.id];
      if (!users) return;
      const i = this.findUserInChannel(users, uId);
      if (i < 0) return;
      return users[i];
    }

    public getChannel(id: string): IChannel | undefined {
      var i = this.getChannelIndex(id);
      if (i < 0) return;
      return this.channels[i];
    }

    public getDmId(ids: string[]): string {
      // dms are currently kind of 'odd'.
      // eventually they will work like real channels
      // but for now we'll just combine keys to make it easier to look up
      if (ids.length < 2) { ids.push(this.userId); }
      return ids.sort().join(".");
    }

    private getChannelIndex(id: string): number {
      for (var i = 0; i < this.channels.length; i++) {
        if (this.channels[i].id == id) {
          return i;
        }
      }
      return -1;
    }

    public updateChannel(id: string, update: (channel: IChannel) => void, create: boolean = false): IChannel | undefined {
      let ch = this.getChannel(id);
      if (!ch && !create) return undefined;
      if (!ch) {
        this.channelMessages[id] = [];
        this.channelUsers[id] = [];
        ch = {
          id: id,
          userId: id,
          name: '',
          description: '',
          isSecret: false,
          lastModified: new Date(),
          allowsGuests: true,
          maxUsers: 0,
          last: -1,
          unread: 0,
          bans: [],
          notifications: [],
          isRoom: false,
          isSyncing: false,
          isSynced: false,
          newMsg: '',
        }
        this.channels.push(ch);
      }
      update(ch);
      this.store.saveChannel(ch);
      return ch;
    }

    public removeChannel(id: string) {

      // find the channel
      var i = this.getChannelIndex(id);
      if (i < 0) return;

      // remove it from everywhere
      this.channels.splice(i, 1);
      this.store.removeChannel(id);
      const msgs = this.channelMessages[id];
      delete this.channelMessages[id];
      delete this.channelUsers[id];

      // clear stored items (that are separate)
      if (msgs) {
        for (let m of msgs) {
          this.store.removeMessage(m.Id);
        }
      }

      // clear if matches
      if (this.channel!.id == id) {
        this.channel = undefined;
      }

      // if channel is null, switch to the first one
      if (!this.channel && this.channels.length > 0) {
        this.switchToChannel(this.channels[0].id);
      }
    }

    private findUserInChannel(users: IChannelUser[], uid: string): number {
      for (let x = 0; x < users.length; x++) {
        if (users[x].id === uid) {
          return x;
        }
      }
      return -1;
    }

    public clearChannelUsers(ch: IChannel) {
      const users = this.channelUsers[ch.id];
      if (!users) return;
      delete this.channelUsers[ch.id];
    }

    public addOrUpdateChannelUser(ch: IChannel, user: IChannelUser) {
      this.removeUserFromChannel(ch, user.id);
      let users = this.channelUsers[ch.id];
      if (!users) {
        users = [];
        this.channelUsers[ch.id] = users;
      };

      users.push(user);
    }

    public removeUserFromChannel(ch: IChannel, uId: string) {
      const users = this.channelUsers[ch.id];
      if (!users) return;
      const r = this.findUserInChannel(users, uId);
      if (r > -1) {
        users.splice(r, 1);
      }
    }

    public addMessageToChannel(ch: IChannel, msg: Response<IChannelResponse>) {
      const data = <IChatMessage><any>msg;

      // check if we haven't seen this user before
      // for message history, it's quite possible
      const msgNick = (msg as Response<ChannelMessageResponse>).Data.Nick;
      const user = this.getUser(msg.Data.UserId);
      if (!user && msgNick) {
        this.updateUser(msg.Data.UserId, (u) => {
          u.nick = msgNick;
        }, true);
      }

      let msgs = this.channelMessages[ch.id];
      if (!msgs) {
        msgs = [];
        this.channelMessages[ch.id] = msgs;
      }

      if (ch.last < msg.Id) {
        ch.last = msg.Id;
      }

      insertSorted(msgs, data, (a, b) => a.Id - b.Id, true);
      this.store.saveMessage(data);

      if (msgs.length > this.MAX_MESSAGES) {
        var trash = msgs.shift();
        this.store.removeMessage(trash!.Id);
      }
    }

    public getChannelLevel(chId: string, uId: string): Rambler.Contracts.Api.ModerationLevel {
      const ch = this.getChannel(chId);
      if (!ch) { return 0; }
      const chuser = this.getChannelUser(ch, uId);
      if (!chuser) return 0;
      return chuser.modLevel;
    }

    public switchToChannel(id: System.Guid) {
      return this.updateChannel(id, ch => {
        this.channel = ch;
        ch.unread = 0;
        this.store.saveSettings(id);
      });
    }

    public getRoomCount() {
      let x = 0;
      for (let ch of this.channels) {
        if (ch.isRoom) x++;
      }
      return x;
    }


    public getUnreadCount() {
      let x = 0;
      for (let ch of this.channels) {
        x += ch.unread;
      }
      return x;
    }

    public disconnect() {
      // marks all channels as out of sync
      // important so it doesn't require a clear on reconnect
      for (let ch of this.channels) {
        ch.isSynced = false;
      }
    }
  }
}
