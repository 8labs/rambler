namespace Rambler {
  export class RamblerApiService {
    static $inject = ['$http', '$window', '$q'];
    constructor(private http: ng.IHttpService, private window: ng.IWindowService, private q: ng.IQService) {}

    // #region Users
    public login(username: string, password: string): ng.IPromise<any> {
      return this.http.post(
        Config.environment.apiUrl + '/account/loginuser',
        { Username: username, Password: password } as Auth,
        { withCredentials: true }
      );
    }

    public loginSocial(provider: string) {
      this.window.location.href = Config.environment.apiUrl + '/account/login?provider=' + provider;
    }

    public register(registration: Rambler.Register): ng.IPromise<any> {
      return this.http.post(Config.environment.apiUrl + '/account/registeruser', registration, {
        withCredentials: true,
      });
    }

    public logout() {
      // Ask webapi to logout.
      return this.http.get(Config.environment.apiUrl + '/account/logout', { withCredentials: true });
    }

    public verifyEmail(validation: Rambler.PasswordReset): ng.IPromise<any> {
      return this.http.post(Config.environment.apiUrl + '/account/validateemail', validation, {
        withCredentials: true,
      });
    }

    public getChatToken(): ng.IPromise<string> {
      const defer = this.q.defer<string>();
      this.http
        .get<string>(Config.environment.apiUrl + '/account/chattoken', { withCredentials: true })
        .then((response) => {
          defer.resolve(response.data);
        })
        .catch((response) => {
          defer.reject(response);
        });

      return defer.promise;
    }

    public getGuestToken(token: string): ng.IPromise<string> {
      return this.http
        .post<string>(Config.environment.apiUrl + '/account/guestchattoken', { data: token })
        .then((r) => r.data);
    }

    public refreshToken(token: string): ng.IPromise<string> {
      const defer = this.q.defer<string>();
      this.http
        .post<string>(Config.environment.apiUrl + '/account/refreshtoken', { Token: token }, { withCredentials: true })
        .then((response) => {
          defer.resolve(response.data);
        })
        .catch((response) => {
          defer.reject(response);
        });

      return defer.promise;
    }

    // #endregion Users

    // #region Channels
    public getOpenChannelList(search: string = ''): ng.IPromise<ListChannel[]> {
      let defer = this.q.defer<ListChannel[]>();
      this.http
        .get(Config.environment.apiUrl + '/chat/getrooms', { params: { search: search }, withCredentials: true })
        .then((response) => {
          defer.resolve(response.data as ListChannel[]);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public getChannelList(search: string = ''): ng.IPromise<ListChannel[]> {
      let defer = this.q.defer<ListChannel[]>();
      this.http
        .get(Config.environment.apiUrl + '/channel/getchannels', { params: { search: search }, withCredentials: true })
        .then((response) => {
          defer.resolve(response.data as ListChannel[]);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public getChannelModeratorList(channelId: System.Guid): ng.IPromise<ChannelModeratorDto[]> {
      let defer = this.q.defer<ChannelModeratorDto[]>();
      this.http
        .post(
          Config.environment.apiUrl + '/channel/getmoderators',
          {},
          { params: { channelId: channelId }, withCredentials: true }
        )
        .then((response) => {
          defer.resolve(response.data as ChannelModeratorDto[]);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public addChannelModerator(
      channelId: System.Guid,
      userId: System.Guid,
      modlevel: number
    ): ng.IPromise<ChannelModeratorDto> {
      let defer = this.q.defer<ChannelModeratorDto>();
      this.http
        .post(
          Config.environment.apiUrl + '/channel/addmoderator',
          {},
          { params: { channelId: channelId, userId: userId, level: modlevel }, withCredentials: true }
        )
        .then((response) => {
          defer.resolve(response.data as ChannelModeratorDto);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public removeChannelModerator(channelId: System.Guid, moderator: ChannelModeratorDto): ng.IPromise<boolean> {
      let defer = this.q.defer<boolean>();
      this.http
        .post(
          Config.environment.apiUrl + '/channel/removemoderator',
          {},
          { params: { channelId: channelId, userId: moderator.UserId }, withCredentials: true }
        )
        .then((response) => {
          defer.resolve(true);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public setChannelModeratorLevel(
      channelId: System.Guid,
      userId: System.Guid,
      modlevel: number
    ): ng.IPromise<ChannelModeratorDto> {
      let defer = this.q.defer<ChannelModeratorDto>();
      this.http
        .post(
          Config.environment.apiUrl + '/channel/setmoderatorlevel',
          {},
          { params: { channelId: channelId, userId: userId, level: modlevel }, withCredentials: true }
        )
        .then((response) => {
          defer.resolve(response.data as ChannelModeratorDto);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public getMyChannelList(search: string = ''): ng.IPromise<ListChannel[]> {
      let defer = this.q.defer<ListChannel[]>();
      this.http
        .get(Config.environment.apiUrl + '/channel/getownedchannels', {
          params: { search: search },
          withCredentials: true,
        })
        .then((response) => {
          defer.resolve(response.data as ListChannel[]);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public addUpdateChannel(channel: Rambler.ChannelDto): ng.IPromise<Rambler.ChannelDto> {
      let defer = this.q.defer<Rambler.ChannelDto>();
      let path = 'registerchannel';
      if (channel.Id) {
        path = 'updatechannel';
      }
      this.http
        .post(Config.environment.apiUrl + '/channel/' + path, channel, { withCredentials: true })
        .then((response) => {
          defer.resolve(response.data as Rambler.ChannelDto);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public registerChannel(channel: Rambler.ChannelDto): ng.IPromise<Rambler.ChannelDto> {
      let defer = this.q.defer<Rambler.ChannelDto>();
      this.http
        .post(Config.environment.apiUrl + '/channel/updatechannel', channel, { withCredentials: true })
        .then((response) => {
          defer.resolve(response.data as Rambler.ChannelDto);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public addChannelBan(channelban: ChannelBanDto): ng.IPromise<ChannelBanDto> {
      let defer = this.q.defer<ChannelBanDto>();
      this.http
        .post(Config.environment.apiUrl + '/channel/addban', channelban, { withCredentials: true })
        .then((response) => {
          defer.resolve(response.data as ChannelBanDto);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public removeChannelBan(channelban: ChannelBanDto): ng.IPromise<boolean> {
      let defer = this.q.defer<boolean>();
      this.http
        .post(
          Config.environment.apiUrl + '/channel/removeban',
          {},
          { params: { channelId: channelban.ChannelId, banId: channelban.Id }, withCredentials: true }
        )
        .then((response) => {
          defer.resolve(true);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public addChannelWarning(channelban: ChannelBanDto): ng.IPromise<ChannelBanDto> {
      let defer = this.q.defer<ChannelBanDto>();
      this.http
        .post(Config.environment.apiUrl + '/channel/warnuser', channelban, { withCredentials: true })
        .then((response) => {
          defer.resolve(response.data as Rambler.ChannelBanDto);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    public getChannelBans(channelId: System.Guid): ng.IPromise<ChannelBanDto[]> {
      let defer = this.q.defer<ChannelBanDto[]>();
      this.http
        .post(
          Config.environment.apiUrl + '/channel/getbans',
          {},
          { params: { channelId: channelId }, withCredentials: true }
        )
        .then((response) => {
          defer.resolve(response.data as ChannelBanDto[]);
        })
        .catch((response) => {
          defer.reject(response);
        });
      return defer.promise;
    }

    // #endregion Channels

    // #region Messages
    public getSubscriptionMessages(token: string, sub: string, last: number) {
      return this.http
        .post(Config.environment.apiUrl + '/chat/subscriptionmessages', { Token: token, Id: sub, Last: last })
        .then((resp) => {
          return resp.data as Response<ChannelMessageResponse>[];
        });
    }

    public getDirectMessages(token: string, userId: string, last: number) {
      return this.http
        .post<any[]>(Config.environment.apiUrl + '/chat/dmmessages', { Token: token, UserId: userId, Last: last })
        .then((resp) => {
          return resp.data as Response<DirectMessageResponse>[];
        });
    }

    public resetPassword(email: string) {
      console.log(email);
      return this.http.post(Config.environment.apiUrl + '/account/RequestPasswordReset', { Email: email });
    }

    public verifyResetPassword(reset: Rambler.PasswordReset): ng.IPromise<any> {
      return this.http.post(Config.environment.apiUrl + '/account/ResetPassword', reset, {
        withCredentials: true,
      });
    }

    // #endregion Messages

    public getUserSettings(): ng.IPromise<UserSettingsDto> {
      return this.http
        .get<UserSettingsDto>(Config.environment.apiUrl + '/chat/getusersettings', { withCredentials: true })
        .then((response) => response.data);
    }

    public addIgnore(uId: string): ng.IPromise<IgnoreDto[]> {
      return this.http
        .post<IgnoreDto[]>(Config.environment.apiUrl + '/chat/addignore', { IgnoreId: uId }, { withCredentials: true })
        .then((response) => response.data);
    }

    public removeIgnore(uId: string): ng.IPromise<IgnoreDto[]> {
      return this.http
        .post<IgnoreDto[]>(
          Config.environment.apiUrl + '/chat/removeignore',
          { IgnoreId: uId },
          { withCredentials: true }
        )
        .then((response) => response.data);
    }
  }
}

angular.module('rambler').service('RamblerApiService', Rambler.RamblerApiService);
