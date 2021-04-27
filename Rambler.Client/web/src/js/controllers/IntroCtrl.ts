namespace Rambler {
  export class IntroCtrl {

    description: string;
    allowGuests = true;
    showCaptcha = false;

    static $inject = ['$location', '$routeParams', 'ChatStateService', 'CoordinationService', 'RamblerApiService', 'AnalyticsService'];

    constructor(private location: ng.ILocationService, private routeParams: ng.route.IRouteParamsService, private chat: ChatStateService, private coordinator: CoordinationService, private api: RamblerApiService, private analyticsService: AnalyticsService) {

      if (routeParams.room) {
        chat.autoJoinChannel = routeParams.room
      }

      if (routeParams.description) {
        this.description = routeParams.description;
      }

      if (routeParams.disallowGuests) {
        this.allowGuests = false;
      }

      if (this.coordinator.hasIdentity()) {
        // Looks like we're already logged in, skip straight to chat.
        this.coordinator.redirectToChat();
        return;
      }

      this.analyticsService.event('IntroLoaded', '', '');
    }

    createAccount() {
      this.location.path('/register');
    }

    loginSocial(provider: string) {
      this.api.loginSocial(provider);
    }

    login() {
      this.coordinator.redirectToLogin();
    }

    onCaptcha(token: string) {
      // grab guest token then move on to chat
      this.api.getGuestToken(token)
        .then(response => {
          // clear and set the token just in case things were weird before this
          this.coordinator.clear();
          this.coordinator.setToken(response);
          this.coordinator.redirectToChat();
        });
    }

    guestLogin() {
      // this.showCaptcha = true;
      this.api.getGuestToken('asdf')
        .then(response => {
          // clear and set the token just in case things were weird before this
          this.coordinator.clear();
          this.coordinator.setToken(response);
          this.coordinator.redirectToChat();
        });
    }
  }
}

angular
  .module('rambler')
  .controller('IntroCtrl', Rambler.IntroCtrl);

