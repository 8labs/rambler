namespace Rambler {
  export class ChatCtrl {
    error: string;

    leftNavDocked: boolean = false;
    rightNavDocked: boolean = false;
    sentAMessage: boolean = false;
    sendWait: boolean = false;
    sendTimes: Date[] = [];
    emojis: IEmoji[] = [];
    selectedEmojiCategory: string = 'People';
    showEmojis: boolean = false;
    contextUser: IChannelUser;
    selectedUser: IChannelUser;
    channels: Rambler.ListChannel[];
    channelListOpen: boolean = false;
    showChannelSearch: boolean = false;
    channelSearchTerm: string;
    channelSearchingDb: boolean = false;
    showChannelAdd: boolean = false;
    editingChannelName: string;
    desktopMenuOpen: boolean = false;
    selectedMessage: IChatMessage;
    userContextMenuVisible: boolean = false;
    removeModeratorMode: boolean = false;
    confirmModerator: boolean = false;

    editingChannel: ChannelDto;
    moderatorList: ChannelModeratorDto[];

    banList: ChannelBanDto[];

    serverBanList: ServerBanDto[];

    newRoomForm: ng.IFormController;

    banLength: number = 1;
    channelBan: ChannelBanDto;
    serverBan: ServerBanDto;

    sinfo: ServerUserInfoDto;

    mobileMenuOpen: boolean = false;

    serverBanReason: string = '';

    showIgnoreList = false;

    userSettings: UserSettingsDto = {
      Ignores: [],
    };

    userListOrderBy: (chuser1: { value: IChannelUser }, chuser2: { value: IChannelUser }) => number;

    // flood controls
    msgRateLimit = 1;
    msgPer = 1000; //unit: ms (messages/ms);
    msgAllowance = this.msgRateLimit;
    msgLastCheck = + new Date();
    lastMsg = '';

    static $inject = ['RamblerApiService', 'ChatStateService', '$location', '$route', '$window', '$scope', '$timeout', '$http', '$cookies', 'AnalyticsService', 'AdminService', 'CoordinationService']
    constructor(private api: RamblerApiService, private chat: ChatStateService, private location: ng.ILocationService, private route: ng.route.IRouteService, private window: ng.IWindowService, private scope: ng.IScope, private timeout: ng.ITimeoutService, private http: ng.IHttpService, private cookies: ng.cookies.ICookiesService, private analytics: AnalyticsService, private admin: AdminService, private coordinator: CoordinationService) {

      if (!this.coordinator.hasIdentity()) {
        this.coordinator.redirectToIntro();
        return;
      }

      this.http.get('assets/emoji/People.json').then((res) => {
        this.chat.state.avatars = res.data as IEmoji[];
        this.connect();
      });

      // get user settings
      if (!this.chat.state.identity!.IsGuest) {
        this.api.getUserSettings().then(settings => {
          this.userSettings = settings;
          this.updateIgnores(settings.Ignores);
        }).catch(e => this.showError("There was an error loading your settings.", e));
      }

      // custom user list comparer
      // 'this' is overriden when called normally
      // also, incoming values are weirdness
      this.userListOrderBy = (chuser1, chuser2) => {
        if (chuser1.value.modLevel != chuser2.value.modLevel) {
          return chuser2.value.modLevel - chuser1.value.modLevel;
        }
        const u1 = this.getUser(chuser1.value.id);
        const u2 = this.getUser(chuser2.value.id);
        return u1!.nick.localeCompare(u2!.nick);
      };
    }

    private showError(msg: string, e?: any) {
      if (e && e.data) {
        this.error = msg + " " + e.data;
      } else {
        this.error = msg;
      }
    }

    setTheme(theme: string) {
      this.chat.setTheme(theme);
    }

    getUser(id: string) {
      return this.chat.state.getUser(id);
    }

    getMyLevel(): Rambler.Contracts.Api.ModerationLevel {
      if (!this.chat.state.channel) return 0;
      return this.chat.state.getChannelLevel(this.chat.state.channel!.id, this.chat.state.userId);
    }

    getUnreadCount() {
      return this.chat.state.getUnreadCount();
    }

    isInChat(): boolean {
      return (this.chat.status == ConnectionStatus.connected) && (!!this.chat.state.channel);
    }

    isGuest(): boolean {
      return !!this.chat.state.identity && this.chat.state.identity.IsGuest;
    }

    isUserGuest(id: string): boolean {
      var user = this.chat.state.getUser(id);
      return !!user && user.isGuest;
    }

    getChannelUsers() {
      if (this.chat.state.channel) {
        return this.chat.state.getChannelUsers(this.chat.state.channel.id);
      }
    }

    getChannelMessages() {
      if (this.chat.state.channel) {
        return this.chat.state.getChannelMessages(this.chat.state.channel.id);
      }
    }

    canCloseCurrentChannel() {
      // borked
      if (!this.chat.state.channel) return false;
      // is a dm, can always close
      if (!this.chat.state.channel.isRoom) return true;
      // not the last room
      if (this.chat.state.getRoomCount() > 1) {
        return true;
      }
    }

    // #region View Helpers

    openChannelList() {
      if (!this.isGuest()) {
        this.desktopMenuOpen = false;
        this.channelListOpen = true;
        this.updateChannelSearch();
      }
    }

    toggleSelectedUser(user: IChannelUser) {
      if (this.isMobile()) {
        this.toggleContextMenu(user);
      } else {
        this.userContextMenuVisible = false;
        if (this.selectedUser === user) {
          delete this.selectedUser;
        } else {
          this.selectedUser = user;
        }
      }
    }

    toggleContextMenu(user: IChannelUser) {
      this.contextUser = user;
      // admins can mod themselves so let them always open the menu
      if (this.chat.state.identity!.Level >= 1000 || this.contextUser!.id != this.chat.state.userId) {
        this.userContextMenuVisible = true;
      }
    }

    clearContextMenu() {
      delete this.contextUser;
      this.userContextMenuVisible = false;
    }

    addIgnore(user: IUser) {
      this.api.addIgnore(user.id)
        .then(ignores => this.updateIgnores(ignores))
        .catch(e => this.showError("Something went wrong adding that ignore.", e));
      this.clearContextMenu();
      this.mobileMenuOpen = false;
    }

    unIgnore(user: IUser) {
      this.userContextMenuVisible = false;
      this.api.removeIgnore(user.id)
        .then(ignores => this.updateIgnores(ignores))
        .catch(e => this.showError("Something went wrong removing that ignore.", e));
      this.clearContextMenu();
      this.mobileMenuOpen = false;
    }

    removeIgnore(ignore: IgnoreDto) {
      this.api.removeIgnore(ignore.IgnoreId)
        .then(ignores => this.updateIgnores(ignores))
        .catch(e => this.showError("Something went wrong removing that ignore.", e));
    }

    openIgnoreList() {
      this.desktopMenuOpen = false;
      this.showIgnoreList = true;
    }

    closeIgnoreList() {
      this.desktopMenuOpen = false;
      this.showIgnoreList = false;
    }

    isUserIgnored(user: IChannelUser) {
      return (!!this.chat.state.ignoreLookup[user.id]);
    }

    private updateIgnores(ignores: IgnoreDto[]) {
      this.userSettings.Ignores = ignores;
      this.chat.state.ignoreLookup = {};
      for (let i of ignores) {
        if (i.IgnoreId != this.chat.state.userId) {
          this.chat.state.ignoreLookup[i.IgnoreId] = i;
        }
      }
    }

    // #endregion View Helpers

    // #region Room List
    updateChannelSearch() {
      this.channelSearchingDb = false;
      this.api.getOpenChannelList(this.channelSearchTerm).then((response) => {
        this.channels = response;
        if ((!response || response.length === 0) && this.channelSearchTerm && this.channelSearchTerm.length >= 3) {
          this.fetchDbChannelSearch();
        }
      });
    }

    fetchDbChannelSearch() {
      this.api.getChannelList(this.channelSearchTerm).then((response) => {
        this.channelSearchingDb = true;
        if (!this.channels) {
          this.channels = [];
        }
        response.forEach((channel, index) => {
          let match = this.channels.find((value) => {
            return value.Name == channel.Name;
          });
          if (!match) {
            this.channels.push(channel);
          }
        })
      });
    }

    getIdNick() {
      return this.chat.state.identity!.Nick;
    }

    joinChannel(channel: Rambler.ListChannel) {
      this.analytics.event('JoinChannel', this.getIdNick(), channel.Name);
      this.channelSearchTerm = "";
      this.showChannelSearch = false;
      this.channelListOpen = false;
      this.chat.chCreateJoin(channel.Name);
      this.chat.state.updateChannel(channel.Id, (ch) => {
        ch.name = channel.Name;
      }, true);
      this.chat.state.switchToChannel(channel.Id);
    }

    switchToChannel(id: string) {
      this.chat.state.switchToChannel(id);
      this.mobileMenuOpen = false;
    }

    addNewChannel() {
      this.analytics.event('CreateChannel', this.getIdNick(), this.editingChannelName);
      this.editingChannel = {
      } as ChannelDto;

      this.editingChannel.AllowGuests = true;
      this.showChannelSearch = false;
      this.channelListOpen = false;
    }

    saveChannel() {
      this.api.addUpdateChannel(this.editingChannel).then((response) => {
        this.chat.chCreateJoin(this.editingChannel.Name);
        delete this.editingChannel;
      }).catch(e => this.showError("Something went wrong saving the room settings.", e));
    }

    // #endregion Room List

    // #region Room Options/Bans/Mods
    editChannel(channel: IChannel) {
      this.editingChannel = {
        AllowGuests: channel.allowsGuests,
        Description: channel.description,
        Id: channel.id,
        IsSecret: channel.isSecret,
        Name: channel.name,
      } as ChannelDto;
    }

    cancelEditChannel() {
      delete this.editingChannel;
      this.editingChannelName = "";
      this.showChannelSearch = false;
      this.channelListOpen = false;
      this.showChannelAdd = false;
    }

    editChannelBans() {
      this.api.getChannelBans(this.chat.state.channel!.id).then((response) => {
        this.banList = response;
      }).catch(e => this.showError("Sorry, there was a problem loading the moderators list.", e));
    }

    removeBan(ban: ChannelBanDto) {
      // Assume it's going to go ok...
      this.banList.splice(this.banList.indexOf(ban), 1);
      this.api.removeChannelBan(ban).then((response) => {
        // Done.. but we'll ignore it.
      }).catch((response) => {
        // Bad stuff happened. Put them back...
        this.banList.push(ban);
        this.error = "Sorry, there was a problem removing that ban.";
      });
    }

    cancelEditBans() {
      delete this.banList;
      delete this.serverBanList;
    }

    editChannelModerators() {
      this.api.getChannelModeratorList(this.chat.state.channel!.id).then((response) => {
        this.moderatorList = response;
      }).catch((response) => {
        // do stuff
        this.error = "Sorry, there was a problem loading the moderator list.";
      });
    }

    toggleModeratorAdmin(moderator: ChannelModeratorDto) {
      let oldLevel = moderator.Level;
      // Assume it's going to go ok...
      moderator.Level = moderator.Level === 10 ? 100 : 10;
      this.api.setChannelModeratorLevel(this.chat.state.channel!.id, moderator.UserId, moderator.Level).then((response) => {
        // Done... but we'll ignore it.
      }).catch((response) => {
        // reset the level for the view.
        moderator.Level = oldLevel;
        this.error = "Sorry, there was a problem updating that moderator.";
      });
    }

    showConfirmModerator() {
      this.userContextMenuVisible = false;
      this.mobileMenuOpen = false;
      this.confirmModerator = true;
    }

    addModerator(user: IChannelUser) {
      this.confirmModerator = false;
      this.api.addChannelModerator(this.chat.state.channel!.id, user.id, 10).then((response) => {
        this.editChannelModerators();
      }).catch((response) => {
        this.error = "Sorry, I was unable to add that person.";
      });
    }

    removeModerator(moderator: ChannelModeratorDto) {
      // Assume it's going to go ok...
      this.moderatorList.splice(this.moderatorList.indexOf(moderator), 1);
      this.api.removeChannelModerator(this.chat.state.channel!.id, moderator).then((response) => {
        // Done.. but we'll ignore it.
      }).catch((response) => {
        // Bad stuff happened. Put them back...
        this.moderatorList.push(moderator);
        this.error = "Sorry, there was a problem removing that moderator.";
      });
    }

    cancelEditModerators() {
      delete this.moderatorList;
      this.removeModeratorMode = false;
    }

    setBanLength(days: number) {
      this.banLength = days;
      let expiry: Date = new Date();
      expiry.setDate(expiry.getDate() + days);
      this.channelBan.Expires = expiry;
    }

    setServerBanLength(days: number) {
      this.banLength = days;
      let expiry: Date = new Date();
      expiry.setDate(expiry.getDate() + days);
      this.serverBan.Expires = expiry;
    }

    banUser(user: IUser | IChannelUser, level: Contracts.Api.BanLevel) {
      this.userContextMenuVisible = false;
      this.mobileMenuOpen = false;

      this.banLength = 1;
      let expiry: Date = new Date();
      expiry.setDate(expiry.getDate() + 1);

      var u = this.chat.state.getUser(user.id);

      this.channelBan = {
        Nick: u!.nick,
        ChannelId: this.chat.state.channel!.id,
        UserId: user.id,
        Reason: "",
        Expires: expiry,
        Level: level
      } as ChannelBanDto;
    }

    cancelBan() {
      delete this.channelBan;
      delete this.serverBan;
    }

    saveBan() {
      if (this.channelBan.Level === 0) {
        this.api.addChannelBan(this.channelBan).then((response) => {
          delete this.channelBan;
        }).catch(e => this.showError("Something went wrong saving that warning.", e));
      } else if (this.channelBan.Level === 1) {
        this.api.addChannelWarning(this.channelBan).then((response) => {
          delete this.channelBan;
        }).catch(e => this.showError("Something went wrong saving that ban.", e));
      }
    }
    // #endregion Room Options/Bans/Mods

    closeServerUserInfo() {
      delete this.sinfo;
    }

    connectError(response: any) {
      console.log('error', response);
      this.coordinator.clear();
      this.coordinator.redirectToIntro();
    }

    connect() {
      this.chat.connect({
        onban: (e) => {
          this.coordinator.clear(true);
          this.serverBanReason = "Server ban reason: " + e.reason;
        },
        onchannelsclosed: () => this.openChannelList(),
        oninvalidtoken: (e) => {
          this.api.refreshToken(this.coordinator.getToken()!)
            .then(response => {
              // get a new token and try again
              this.coordinator.setToken(response);
              this.connect();
            }).catch(this.connectError);
        },
      });

      this.analytics.event('ChatLoaded', this.getIdNick(), '');
    }

    logout() {
      this.api.logout();
      this.coordinator.clear();
      this.coordinator.redirectToIntro();
    }

    goRegister() {
      this.api.logout();
      this.coordinator.clear();
      this.location.path('/register');
    }

    clearBanNotice() {
      delete this.chat.state.ban;
    }

    clearError() {
      let fatal: boolean = false;
      if (this.chat.state.error_code === 1 || this.chat.state.error_code === 2 || this.chat.state.error_code === 3) {
        fatal = true;
      }

      delete this.chat.state.error_code;
      delete this.error;

      if (fatal) {
        this.logout();
      }
    }

    public closeCurrent() {
      const current = this.chat.state.channel
      if (!current) return;
      if (current.isRoom) {
        this.chat.chRequestPart(current.id);
      }
      this.chat.state.removeChannel(current.id);
    }

    openDm(user: IUser): IChannel | undefined {
      this.userContextMenuVisible = false;
      if (!user || user.id == this.chat.state.userId) return;
      const id = this.chat.state.getDmId([user.id, this.chat.state.userId]);
      const ch = this.chat.state.updateChannel(id, (ch) => {
        ch.isRoom = false;
        ch.name = user.nick;
        ch.userId = user.id;
        this.chat.loadPrivateMessages(user.id, ch.last);
      }, true);
      this.clearContextMenu();
      this.switchToChannel(ch!.id);
    }

    // #region Input handling

    inputKeyPress(e: KeyboardEvent) {
      const ch = this.chat.state.channel;
      if (!ch) return;

      let keyCode = (e.keyCode ? e.keyCode : e.which);
      // Tab to nick complete!
      if (keyCode == 9) {
        e.preventDefault();
        const users = this.chat.state.channelUsers[this.chat.state.channel!.id];
        if (!users) return;
        const user = users
          .map((u) => this.getUser(u.id))
          .find((u) => {
            let words = ch.newMsg.toLowerCase().split(' ');
            return u!.nick.toLowerCase().startsWith(words[words.length - 1])
          });
        if (user) {
          ch.newMsg = ch.newMsg.substring(0, ch.newMsg.lastIndexOf(' ') + 1) + user.nick;
        }
      } else if (keyCode == 13) {
        this.send();
      }
    }

    floodTest(): boolean {
      this.sendTimes.push(new Date());
      if (this.sendTimes.length < 4) {
        return false;
      } else {
        // If they've sent 4 messages in 5 seconds, they're flooding.
        if (this.sendTimes[0].getTime() > this.sendTimes[3].getTime() - 5000) {
          console.log("More than 4 messages in 5 seconds... not nice.");
          // Remove last time.
          this.sendTimes.splice(0, 1);
          return true;
        } else {

          // Remove last time.
          this.sendTimes.splice(0, 1);
          return false;
        }
      }
    }

    sendChCommand(ch: IChannel) {

      if (ch.newMsg.startsWith("/shrug")) {
        ch.newMsg = String.fromCharCode(175, 92, 95, 40, 12484, 41, 95, 47, 175) + ch.newMsg.replace("/shrug", "");
        this.sendChMsg(ch);

      } else if (ch.newMsg.startsWith("/tableflip")) {
        ch.newMsg = String.fromCharCode(40, 9583, 176, 9633, 176, 65289, 9583, 65077, 32, 9531, 9473, 9531) + ch.newMsg.replace("/tableflip", "");
        this.sendChMsg(ch);

      } else if (ch.newMsg === "/list") {
        this.openChannelList();

      } else if (ch.newMsg.startsWith("/join") && !this.isGuest()) {
        const msgArr: string[] = ch.newMsg.split(" ");
        if (msgArr.length === 2) {
          this.chat.chCreateJoin(msgArr[1]);
        }

      } else if (ch.newMsg.startsWith("/msg")) {
        const msgArr: string[] = ch.newMsg.split(" ");
        if (msgArr.length >= 2) {
          const user = this.chat.state.getUserByNick(msgArr[1]);
          if (!user) return;
          const dm = this.openDm(user);
          if (dm) {
            // add the rest of the message
            dm.newMsg = msgArr.slice(2).join(" ");
            this.sendChMsg(dm);
          }
        }
      } else if (ch.newMsg.startsWith("/ignore")) {
        const msgArr: string[] = ch.newMsg.split(" ");
        if (msgArr.length >= 2) {
          const user = this.chat.state.getUserByNick(msgArr[1]);
          if (!user) return;
          this.addIgnore(user);
        }

      } else if (ch.newMsg.startsWith("/ban")) {
        const level = this.chat.state.getChannelLevel(ch.id, this.chat.state.userId);
        if (level >= Rambler.Contracts.Api.ModerationLevel.Moderator) {
          const msgArr: string[] = ch.newMsg.split(" ");
          if (msgArr.length >= 2) {
            const user = this.chat.state.getUserByNick(msgArr[1]);
            if (!user) return;
            this.banUser(user, 0);
            if (msgArr.length > 2) {
              msgArr.splice(0, 2);
              let reason = msgArr.join(" ");
              this.channelBan.Reason = reason;
              this.saveBan();
            }
          }
        }

      } else if (ch.newMsg.startsWith("/warn")) {
        const level = this.chat.state.getChannelLevel(ch.id, this.chat.state.userId);
        if (level >= Rambler.Contracts.Api.ModerationLevel.Moderator) {
          const msgArr: string[] = ch.newMsg.split(" ");
          if (msgArr.length >= 2) {
            const user = this.chat.state.getUserByNick(msgArr[1]);
            if (!user) return;
            this.banUser(user, 1);
            if (msgArr.length > 2) {
              msgArr.splice(0, 2);
              let reason = msgArr.join(" ");
              this.channelBan.Reason = reason;
              this.saveBan();
            }
          }
        }

      } else if (ch.newMsg.startsWith("/sban")) {
        const level = this.chat.state.identity!.Level;
        if (level >= Rambler.Contracts.Api.ModerationLevel.ServerAdmin) {
          const msgArr: string[] = ch.newMsg.split(" ");
          if (msgArr.length === 2) {
            const nick = msgArr[1];
            const user = this.chat.state.getUserByNick(msgArr[1]);
            const uid = user ? user.id : undefined;
            this.addServerBan(nick, uid);
            if (msgArr.length > 2) {
              msgArr.splice(0, 2);
              let reason = msgArr.join(" ");
              this.serverBan.Reason = reason;
              this.saveServerBan();
            }
          } else {
            this.admin.getbans().then((response) => {
              this.serverBanList = response;
            });
          }
        }
      } else if (ch.newMsg.startsWith("/sinfo")) {
        const level = this.chat.state.identity!.Level;
        if (level >= Rambler.Contracts.Api.ModerationLevel.ServerAdmin) {
          const msgArr: string[] = ch.newMsg.split(" ");
          if (msgArr.length === 2) {
            this.admin.getUserInfo(msgArr[1]).then((response) => {
              this.sinfo = response;
            });
          }
        }
      } else if (ch.newMsg.startsWith("/sadmin")) {
        const level = this.chat.state.identity!.Level;
        if (level >= Rambler.Contracts.Api.ModerationLevel.ServerAdmin) {
          const msgArr: string[] = ch.newMsg.split(" ");
          if (msgArr.length >= 2) {
            const up = msgArr[1].toLowerCase() != "down";
            const nick = msgArr.length >= 3 ? msgArr[2] : undefined;
            this.admin.toggleAdmin(up, nick);
          }
        }
      } else {
        this.showError("I don't know that command");
      }
    }

    isOverMsgAllowance() {
      var current = + new Date();
      var passed = current - this.msgLastCheck;
      this.msgLastCheck = current;
      this.msgAllowance += passed * (this.msgRateLimit / this.msgPer);
      if (this.msgAllowance > this.msgRateLimit) this.msgAllowance = this.msgRateLimit;
      return this.msgAllowance < 1;
    }

    isMsgDupe(msg: string) {
      const dist = getFuzzyDistance(this.lastMsg, msg);
      // for every 10 characters, fuzzy match one more diff character
      const fuzz = msg.length / 10;
      return (dist <= fuzz);
    }

    sendChMsg(ch: IChannel) {
      // Don't send empty messages.
      if (!ch.newMsg || ch.newMsg === '') {
        console.log("Message empty.");
        return;
      }

      if (ch.newMsg.startsWith('/')) {
        this.sendChCommand(ch);
        ch.newMsg = '';
        return;
      }

      // Don't send messages faster than 1 per second.
      if (this.sendWait) {
        console.log("Sent too recently.");
        return;
      }

      if (this.isMsgDupe(ch.newMsg)) {
        console.log("duplicate message");
        return;
      }

      // Set a timer for 1 second.
      this.sendWait = true;
      this.timeout(() => {
        this.sendWait = false;
      }, 1000);

      // Don't send long messages.
      if (ch.newMsg && ch.newMsg.length > 500) {
        console.log("Message too long.");
        return;
      }

      // Don't send more than 4 messages in 5 seconds.
      if (this.floodTest()) {
        return;
      }

      if (!this.cookies.get("FirstMessage")) {
        let expiry: Date = new Date();
        expiry.setMonth(expiry.getMonth() + 24);
        this.cookies.put("FirstMessage", new Date().toISOString(), { expires: expiry });
        this.analytics.event('FirstMessage', this.getIdNick(), '');
      }

      if (ch.isRoom) {
        this.chat.socket.sender.chanMsg(ch.id, ch.newMsg);
      } else {
        this.chat.socket.sender.directMessage(ch.userId, ch.newMsg);
      }

      this.lastMsg = ch.newMsg;
      ch.newMsg = '';
    }

    send() {
      if (!this.chat.state.channel) { return; }
      this.sendChMsg(this.chat.state.channel);
      let editor = this.getEditor();
      editor.focus();
    }

    private fetchEmojis(category: string) {

      this.selectedEmojiCategory = category;
      this.http.get('assets/emoji/' + category + '.json').then((res) => {
        this.emojis = res.data as IEmoji[];
      });

    }

    private insertEmoji(emoji: string) {
      if (!this.chat.state.channel) { return; }
      const ch = this.chat.state.channel;

      let editor = this.getEditor();
      editor.focus();
      let cursorPos = editor.selectionStart;
      if (!ch.newMsg) {
        ch.newMsg = "";
      }
      let textBefore = ch.newMsg.substring(0, editor.selectionStart!);
      let textAfter = ch.newMsg.substring(editor.selectionEnd!, ch.newMsg.length);
      ch.newMsg = textBefore + emoji + textAfter;
      this.showEmojis = false;
    }

    private getEditor() {
      const editor = document.getElementsByClassName("chat-msg-input-box")[0] as HTMLInputElement;
      return editor;
    }

    // #endregion Input handling

    private isMobile() {
      return !window.matchMedia("(min-width: 48em)").matches;
    }

    private hideNavsOnMobile() {
      if (this.isMobile()) {
        this.leftNavDocked = false;
        this.rightNavDocked = false;
      }
    }

    // #region Admin Stuff

    serverBanUser(user: IUser) {
      // from modal
      this.addServerBan(user.nick, user.id);
    }

    addServerBan(nick: string, uid?: string) {
      this.userContextMenuVisible = false;
      this.mobileMenuOpen = false;
      this.banLength = 1;
      const expiry: Date = new Date();
      expiry.setDate(expiry.getDate() + 1);

      this.serverBan = {
        BannedNick: nick,
        BannedUserId: uid,
        Expires: expiry,
        IPFilter: "",
        Reason: ""
      } as ServerBanDto;
      console.log('yo', this.serverBan);
    }

    saveServerBan() {
      this.admin.addServerBan(this.serverBan).then((response) => {
        delete this.serverBan;
      }).catch((response) => {
        this.error = "Something went wrong while banning " + this.serverBan.BannedNick + ".";
      });
    }

    removeServerBan(ban: ServerBanDto) {
      // Assume it's going to go ok...
      this.serverBanList.splice(this.serverBanList.indexOf(ban), 1);
      this.admin.removeServerBan(ban).then((response) => {
        // Done.. but we'll ignore it.
      }).catch((response) => {
        // Bad stuff happened. Put them back...
        this.serverBanList.push(ban);
        this.error = "Sorry, there was a problem removing that ban.";
      });
    }
    // #endregion Admin Stuff
  }
}

angular
  .module('rambler')
  .controller('ChatCtrl', Rambler.ChatCtrl);
