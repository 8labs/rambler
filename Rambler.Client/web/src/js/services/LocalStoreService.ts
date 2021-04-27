namespace Rambler {

  interface ISettingsStore {
    channelId: string; //current channel
  }

  interface IExpire {
    expires: number;
  }

  export class LocalStoreService {

    public Expires = 1000 * 60 * 60 * 24; // one day in ms

    constructor() { }

    public load(state: ChatState) {
      this.loadStoredItems(state);
      const token = this.getToken();
      if (token) {
        state.parseToken(token);
      }
      var settings = this.loadSettings();
      if (settings) {
        state.channel = state.getChannel(settings.channelId)!;
      }

      if (!state.channel && state.channels.length > 0) {
        state.switchToChannel(state.channels[0].id);
      }
    }

    public getToken() {
      return localStorage.getItem("TOKEN");
    }

    public saveToken(token: string) {
      return localStorage.setItem("TOKEN", token);
    }

    public removeToken() {
      localStorage.removeItem("TOKEN");
    }

    public saveSettings(channelId: string) {
      localStorage.setItem("SETTINGS", JSON.stringify(<ISettingsStore>{ channelId }));
    }

    public loadSettings(): ISettingsStore | undefined {
      const v = localStorage.getItem("SETTINGS");
      if (v) {
        return JSON.parse(v);
      }
      return undefined;
    }

    public saveChannel(ch: IChannel) {
      this.setExpires(ch);
      localStorage.setItem("CH_" + ch.id, JSON.stringify(ch));
    }

    public removeChannel(id: string) {
      localStorage.removeItem("CH_" + id);
    }

    private loadStoredItems(state: ChatState) {
      const delKeys: string[] = [];
      const now = +new Date();
      const messages = [];
      state.channels = [];
      state.users = {};

      for (let i = 0; i < localStorage.length; ++i) {
        const k = localStorage.key(i)!;
        const v = localStorage.getItem(k);
        if (k != "TOKEN" && k != "THEME" && v) {
          const value = JSON.parse(v);
          delete value.$$hashKey; // delete any angular crap.
          if (this.isExpired(value, now)) {
            // expired item, don't load
            delKeys.push(k);

          } else {

            if (k.startsWith("CH_")) {
              const ch = value as IChannel;
              ch.isSynced = false; // lets the client know if this needs to be refreshed or not
              ch.isSyncing = false; // in process of refreshing
              state.channels.push(ch);

            } else if (k.startsWith("U_")) {
              const u = value as IUser;
              state.users[u.id] = u;
              state.userNickLookup[u.nick] = u.id;

            } else if (k.startsWith("MSG_")) {
              messages.push(value);
            }
          }
        }
      }

      // add all the messages after everything else is loaded
      for (let msg of messages) {
        let ch = state.getChannel(msg.Subscription);
        if (!ch) {
          // might be a dm
          const otherUserId = msg.Data.EchoUser ? msg.Data.EchoUser : msg.Data.UserId;
          const dmId = state.getDmId([msg.Subscription, otherUserId]);
          ch = state.getChannel(dmId);
        }

        if (ch) {
          let chMsgs = state.channelMessages[ch.id];
          if (!chMsgs) {
            chMsgs = [];
            state.channelMessages[ch.id] = chMsgs;
          }
          insertSorted(chMsgs, msg, (a, b) => a.Id - b.Id, true);
        } else {
          // not in this channel, trash it
          this.removeMessage(msg);
        }
      }

      // remove all the expired items when it's done
      for (let k of delKeys) {
        localStorage.removeItem(k);
      }
    }

    public saveUser(user: IUser) {
      //need this info on reload pretty often
      //if (user.isGuest || user.isSelf) return;
      this.setExpires(user);
      localStorage.setItem("U_" + user.id, JSON.stringify(user));
    }

    public removeUser(id: string) {
      localStorage.removeItem("U_" + id);
    }

    public saveMessage(msg: IChatMessage) {
      localStorage.setItem("MSG_" + msg.Id, JSON.stringify(msg));
    }

    public removeMessage(id: number) {
      localStorage.removeItem("MSG_" + id);
    }

    private setExpires(item: any) {
      (item as any).expires = Date.now() + this.Expires;
    }

    private isExpired(item: any, now: number) {
      (item as any).expires > now;
    }

    public getTheme(): string {
      return localStorage.getItem("THEME") || "ewc";
    }

    public saveTheme(theme: string) {
      return localStorage.setItem("THEME", theme);
    }

    public clear(saveToken: boolean) {
      if (saveToken) {
        const token = this.getToken();
        localStorage.clear();
        this.saveToken(token!);
      } else {
        localStorage.clear();
      }
    }
  }
}

angular
  .module('rambler')
  .service('LocalStoreService', Rambler.LocalStoreService);
