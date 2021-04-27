namespace Rambler {

  export interface IChannelMessageHandler {
    [messageType: string]: (ch: IChannel, msg: IResponse) => void;
  }

  export interface IUserMessageHander {
    [messageType: string]: (msg: IResponse) => void;
  }

  export enum ConnectionStatus {
    disconnected = "DISCONNECTED",
    connected = "CONNECTED",
    connecting = "CONNECTING",
    waiting = "WAITING",
  }

  // general events from this thing
  // passed into the connect handler
  export interface IChatStateHandler {
    oninvalidtoken: (e: CloseEvent) => void;
    onban: (e: CloseEvent) => void;
    onchannelsclosed: () => void;
  }

  export class ChatStateService {

    public state: ChatState;

    public status: ConnectionStatus = ConnectionStatus.disconnected;

    public statusEvents: IChatStateHandler;

    private channelEvents: IChannelMessageHandler = {};
    private userEvents: IUserMessageHander = {};

    private isDisconnected = false;
    private isJoining = false;
    public autoJoinChannel: string;

    public theme: string = "ewc";

    static $inject = ['$http', '$timeout', 'SocketService', 'RamblerApiService', 'LocalStoreService'];
    constructor(private http: ng.IHttpService, private timeout: ng.ITimeoutService, public socket: SocketService, public api: RamblerApiService, private store: LocalStoreService) {

      // setup the channel event handlers
      this.channelEvents[SocketService.CHJOIN] = this.chJoin as any;
      this.channelEvents[SocketService.CHMSG] = this.chMsg as any;
      this.channelEvents[SocketService.CHUPDATE] = this.chUpdate as any;
      this.channelEvents[SocketService.CHUSERUPDATE] = this.chUserUpdate as any;

      // and the general/user event handlers (includes dms)
      this.userEvents[SocketService.JOIN] = this.join as any;
      this.userEvents[SocketService.AUTH] = this.auth as any;
      this.userEvents[SocketService.CHUSERS] = this.chUsers as any;
      this.userEvents[SocketService.CHBAN] = this.chBan as any;
      this.userEvents[SocketService.CHPART] = this.chPart as any;

      this.userEvents[SocketService.ERROR] = this.error as any;
      this.userEvents[SocketService.DM] = this.dm as any;

      this.state = new ChatState(this.store);
      this.store.load(this.state);

      this.theme = this.store.getTheme();

      this.socket.onclose = (e) => {
        logMsg('State: ' + this.socket.getState() + ' (close)');

        this.status = ConnectionStatus.disconnected;

        // this prepares the state to deal with reconnection
        this.state.disconnect();

        if (e.code == 1007) {
          // this is a bad auth close.
          // we should try to get a new tokens
          console.log("invalid token", e);
          this.statusEvents.oninvalidtoken(e);

        } else if (e.code == 1008) {
          // on server ban, they should still get a notice before disconnect
          console.log('server ban', e);
          this.statusEvents.onban(e);

        } else if (!this.isDisconnected) {
          // As we don't actually have a close method of any sort, this probably wasn't intentional... let's reconnect.
          this.status = ConnectionStatus.waiting;
          this.timeout(() => {
            this.connect(this.statusEvents);
          }, 2000);
        }
      };

      this.socket.onopen = (e) => {
        this.status = ConnectionStatus.connected;
        logMsg('State: ' + this.socket.getState() + ' (open)');
      };

      this.socket.onerror = (e) => {
        logMsg('State: ' + this.socket.getState() + ' (error)');
      };

      this.socket.onmessage = (e) => { }

      this.socket.onresponse = (msg) => this.handleMessage(msg);

      const logMsg = (msg: string) => {
        console.log(msg);
      }
    }

    public disconnect() {
      this.isDisconnected = true;
      this.socket.disconnect();
      this.state.disconnect();
    }

    public reset() {
      this.state = new ChatState(this.store);
      this.store.load(this.state);
    }

    public connect(events: IChatStateHandler) {
      this.statusEvents = events;
      this.isDisconnected = false;
      this.status = ConnectionStatus.connecting;
      this.socket.connect(this.state.getToken()!);
    }

    private handleMessage(msg: IResponse) {
      if (this.channelEvents[msg.Type]) {
        this.channelMessage(msg);
      } else if (this.userEvents[msg.Type]) {
        this.userEvents[msg.Type].apply(this, [msg]);
      } else {
        console.log('unknown type', msg.Type, msg);
      }
    }

    private channelMessage(msg: IResponse) {
      this.state.updateChannel(msg.Subscription, (ch) => {
        this.channelEvents[msg.Type].apply(this, [ch, msg]);
        if (ch.last < msg.Id) { ch.last = msg.Id; }
      });
    }

    public loadPrivateMessages(userId: string, last: number) {
      // start the process to get old messages
      this.api.getDirectMessages(this.state.getToken()!, userId, last)
        .then(responses => {
          const id = this.state.getDmId([this.state.userId, userId]);
          this.state.updateChannel(id, (ch) => {
            for (let x = responses.length - 1; x >= 0; x--) {
              this.state.addMessageToChannel(ch, responses[x] as Response<IChannelResponse>);
            }
            ch.isSynced = true;
          });
        });
    }

    private dm(msg: Response<DirectMessageResponse>) {
      // data.userid = sender
      // data.echouser = if set, this is the other dm participant and userid = you.
      // subscriber = always you
      const otherUserId = msg.Data.EchoUser ? msg.Data.EchoUser : msg.Data.UserId;
      const otherUser = this.state.getUser(otherUserId);

      if (!otherUserId || !otherUser) {
        console.log("Error processing DM", otherUserId, msg);
        return;
      }

      var isIgnored = this.state.ignoreLookup[otherUserId];

      const dmId = this.state.getDmId([this.state.userId, otherUserId]);
      this.state.updateChannel(dmId, (ch) => {
        ch.isRoom = false;
        ch.userId = otherUserId;
        ch.name = otherUser.nick;

        if (!ch.isSyncing && !ch.isSynced) {
          // start the process to get the previous old messages
          ch.isSyncing = true;  // don't let subsequent messages make it angry
          this.loadPrivateMessages(otherUserId, ch.last);
        }
        this.state.addMessageToChannel(ch, msg);

        if (ch.id != this.state.channel!.id) {
          ch.unread++;
        }

      }, !isIgnored); // creates if not ignored.
    }

    public chRequestPart(chId: string) {
      this.socket.sender.partById(chId);
    }

    public chCreateJoin(name: string) {
      this.isJoining = true;
      this.socket.sender.joinByName(name);
    }

    private chJoin(ch: IChannel, msg: Response<ChannelJoinedResponse>) {
      // update the user cache
      var u = this.state.updateUser(msg.Data.UserId, (u) => {
        u.nick = msg.Data.Nick;
        u.isGuest = msg.Data.IsGuest;
        u.isSelf = msg.Data.UserId == this.state.userId;
      }, true);

      const chuser = <IChannelUser>{
        id: msg.Data.UserId,
        modLevel: msg.Data.Level,
      };

      // update the nick and add them to the channel, push the message
      this.state.addOrUpdateChannelUser(ch, chuser);
    }

    private chUpdate(ch: IChannel, msg: Response<ChannelUpdateResponse>) {
      this.state.updateChannel(msg.Subscription, (ch) => {
        ch.description = msg.Data.Description;
        ch.name = msg.Data.Name;
        ch.isSecret = msg.Data.IsSecret;
        ch.maxUsers = msg.Data.MaxUsers;
        ch.lastModified = msg.Data.LastModified;
        ch.allowsGuests = msg.Data.AllowsGuests;
      });
    }

    private chUserUpdate(ch: IChannel, msg: Response<ChannelUserUpdateResponse>) {
      // update the user cache
      var u = this.state.updateUser(msg.Data.UserId, (u) => {
        u.nick = msg.Data.Nick;
        u.isGuest = msg.Data.IsGuest;
        u.isSelf = msg.Data.UserId == this.state.userId;
      }, true);

      const chuser = <IChannelUser>{
        id: msg.Data.UserId,
        modLevel: msg.Data.Level,
      };

      // update the nick and add them to the channel, push the message
      this.state.addOrUpdateChannelUser(ch, chuser);
    }

    private chPart(msg: Response<ChannelPartResponse>) {
      // part doesn't update the cached channel state
      // just users and maybe removes it
      const ch = this.state.getChannel(msg.Subscription);
      if (!ch) return;  // already gone from channel
      if (this.state.userId == msg.Data.UserId) {
        // we're being parted
        // This probably is happening in another window
        this.state.removeChannel(ch.id);
      } else {
        // normal user part
        this.state.removeUserFromChannel(ch, msg.Data.UserId);
      }
    }

    private chBan(msg: Response<ChannelBannedResponse>) {
      // ban doesn't update the cached channel state
      // just messages and maybe removes it
      const ch = this.state.getChannel(msg.Subscription);
      if (this.state.userId === msg.Data.UserId) {
        // You've been bad...
        this.state.ban = msg.Data;
      }
    }

    private chMsg(ch: IChannel, msg: Response<ChannelMessageResponse>) {
      this.state.addMessageToChannel(ch, msg);
      if (ch.id != this.state.channel!.id) {
        ch.unread++;
      }
    }

    private chUsers(msg: Response<ChannelUsersResponse>) {
      // chusers is a user message that works with a channel
      // goes a slightly different route than the other ch messages

      const ch = this.state.getChannel(msg.Data.ChannelId);
      if (!ch) return;  // wat?

      // this includes all of 'em, start fresh
      this.state.clearChannelUsers(ch);

      for (let user of msg.Data.Users) {
        // update the user cache
        this.state.updateUser(user.Id, (u) => {
          u.nick = user.Nick;
          u.isSelf = user.Id == this.state.userId;
          u.isGuest = user.IsGuest;
        }, true);

        //and to the room
        const chuser: IChannelUser = {
          id: user.Id,
          modLevel: user.ModLevel,
        };
        this.state.addOrUpdateChannelUser(ch, chuser);
      }
    }

    public loadChannelMessages(chId: string, last: number) {
      // start the process to get old messages
      this.api.getSubscriptionMessages(this.state.getToken()!, chId, last)
        .then(responses => {
          this.state.updateChannel(chId, (ch) => {
            for (let x = responses.length - 1; x >= 0; x--) {
              this.state.addMessageToChannel(ch, responses[x] as Response<IChannelResponse>)
            }
            ch.isSynced = true;
            ch.isSyncing = false;
          });
        });
    }

    private join(msg: Response<JoinResponse>) {
      var chan = this.state.updateChannel(msg.Data.ChannelId, (ch) => {
        ch.name = msg.Data.Name;
        ch.description = msg.Data.Description;
        ch.isRoom = true;
        ch.isSecret = msg.Data.IsSecret;
        ch.maxUsers = msg.Data.MaxUsers;
        ch.allowsGuests = msg.Data.AllowsGuests;
        if (!ch.isSyncing && !ch.isSynced) {
          // start the process to get the previous old messages
          ch.isSyncing = true;
          this.loadChannelMessages(msg.Data.ChannelId, ch.last);
        }
      }, true);

      // add the room if we're not already in one, or requested a join
      if (this.isJoining || !this.state.channel) {
        // note:  this doesn't really deal with errors, etc.
        // could cause funky behavior, but should work most of the time
        console.log(this.isJoining, this.state.channel);
        this.isJoining = false;
        this.state.switchToChannel(chan!.id);
      }

      // start the process to get the users
      // NOTE: this is happening automatically on the server for now.
      //this.socket.sender.chanUsers(msg.Data.ChannelId);
    }

    private auth(msg: Response<AuthResponse>) {
      this.state.userId = msg.Data.UserId;
      this.state.updateUser(msg.Data.UserId, (u) => {
        u.nick = msg.Data.Nick;
        u.isSelf = true;
      }, true);

      let joining = false;
      if (msg.Data.ConnectionCount > 1) {
        for (let ch of this.state.channels) {
          if (ch.isRoom) {
            console.log('rejoining', ch.name);
            this.socket.sender.joinById(ch.id);
            joining = true;
          }
        }
      }

      // Always autojoin embed/direct link room
      if (this.autoJoinChannel) {
        this.socket.sender.joinByName(this.autoJoinChannel);
      }

      if (!joining) {
        if (this.state.getRoomCount() > 0) {
          this.state.channels.forEach((channel) => {
            if (channel.isRoom) {
              this.socket.sender.joinByName(channel.name);
            }
          });
        } else {
          if (!this.autoJoinChannel) {
            this.socket.sender.joinByName("Lobby");
          }
        }
      }
    }

    private error(msg: Response<ErrorResponse>) {
      this.state.error_code = msg.Data.Code;
    }

    setTheme(theme: string) {
      this.theme = theme;
      this.store.saveTheme(theme);
    }

  }

}

angular
  .module('rambler')
  .service('ChatStateService', Rambler.ChatStateService);
