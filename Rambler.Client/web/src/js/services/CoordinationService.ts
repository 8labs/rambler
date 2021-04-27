namespace Rambler {

  export class CoordinationService {

    static $inject = ['$location', 'ChatStateService', 'LocalStoreService'];

    constructor(private location: ng.ILocationService, private chat: ChatStateService, private store: LocalStoreService) {

    }

    public getToken() {
      return this.chat.state.getToken();
    }

    public setToken(token: string) {
      this.chat.state.setToken(token);
    }

    public hasIdentity(): boolean {
      return !!this.chat.state.identity;
    }

    public clear(saveToken: boolean = false) {
      // bad auth, etc.  clear everything, reset
      this.chat.disconnect();
      this.store.clear(saveToken);
      this.chat.reset();
    }

    public redirectToIntro() {
      this.location.path('/');
    }

    public redirectToLogin() {
      this.location.path('/login');
    }

    public redirectToChat() {
      this.location.path('/chat');
    }
  }
}

angular
  .module('rambler')
  .service('CoordinationService', Rambler.CoordinationService)
