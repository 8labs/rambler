namespace Rambler {

  export class AdminService {

    static $inject = ['$http', '$window', '$q'];
    constructor(private http: ng.IHttpService, private window: ng.IWindowService, private q: ng.IQService) {
    }

    public toggleAdmin(up: boolean, nick?: string): ng.IPromise<any> {
      return this.http.post(Config.environment.apiUrl + '/management/toggleadmin',
        {},
        {
          params: { up, nick },
          withCredentials: true
        }
      );
    }

    public getUserInfo(nick: string): ng.IPromise<ServerUserInfoDto> {
      let defer = this.q.defer<ServerUserInfoDto>();
      this.http.get(Config.environment.apiUrl + '/management/getuserinfo',
        { params: { nick }, withCredentials: true }
      ).then((response) => {
        defer.resolve(response.data as ServerUserInfoDto);
      }).catch((response) => {
        defer.reject(response);
      });
      return defer.promise;
    }

    public getbans(): ng.IPromise<ServerBanDto[]> {
      let defer = this.q.defer<ServerBanDto[]>();
      this.http.get(Config.environment.apiUrl + '/management/getserverbans',
        { withCredentials: true }
      ).then((response) => {
        defer.resolve(response.data as ServerBanDto[]);
      }).catch((response) => {
        defer.reject(response);
      });
      return defer.promise;
    }

    public addServerBan(ban: ServerBanDto): ng.IPromise<ServerBanDto> {

      let defer = this.q.defer<ServerBanDto>();
      this.http.post(Config.environment.apiUrl + '/management/addserverbanforuser',
        ban,
        { withCredentials: true }
      ).then((response) => {
        defer.resolve(response.data as ServerBanDto);
      }).catch((response) => {
        defer.reject(response);
      });
      return defer.promise;
    }

    public updateServerBan(ban: ServerBanDto): ng.IPromise<ServerBanDto> {

      let defer = this.q.defer<ServerBanDto>();
      this.http.post(Config.environment.apiUrl + '/management/updateserverbanforuser',
        ban,
        { withCredentials: true }
      ).then((response) => {
        defer.resolve(response.data as ServerBanDto);
      }).catch((response) => {
        defer.reject(response);
      });
      return defer.promise;
    }

    public removeServerBan(ban: ServerBanDto): ng.IPromise<boolean> {

      let defer = this.q.defer<boolean>();
      this.http.post(Config.environment.apiUrl + '/management/removeserverban',
        {},
        { params: { id: ban.Id }, withCredentials: true }
      ).then((response) => {
        defer.resolve(true);
      }).catch((response) => {
        defer.reject(response);
      });
      return defer.promise;
    }
  }
}

angular
  .module('rambler')
  .service('AdminService', Rambler.AdminService);
