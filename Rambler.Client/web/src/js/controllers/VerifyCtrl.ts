namespace Rambler {
  export class VerifyCtrl {
    isValidating: boolean = true;
    error: boolean;
    errorMessage: string;

    static $inject = [
      '$location',
      '$routeParams',
      'ChatStateService',
      'CoordinationService',
      'RamblerApiService',
      'AnalyticsService',
    ];

    constructor(
      private location: ng.ILocationService,
      private routeParams: ng.route.IRouteParamsService,
      private chat: ChatStateService,
      private coordinator: CoordinationService,
      private api: RamblerApiService,
      private analyticsService: AnalyticsService
    ) {
      if (routeParams.code && routeParams.email) {
        this.isValidating = true;
        let verification: Rambler.PasswordReset = {
          Email: routeParams.email,
          NewPassword: '',
          Token: routeParams.code,
          Captcha: '',
        };

        this.analyticsService.event('EmailVerification started', routeParams.email, '');

        api
          .verifyEmail(verification)
          .then((response) => {
            this.error = false;
            this.api.getChatToken().then((response) => {
              this.coordinator.clear();
              this.coordinator.setToken(response);
              this.coordinator.redirectToChat();
            }).catch((reason) => {
              this.analyticsService.event('ValidationLoginFailed', routeParams.email, '');
              this.error = true;
              this.errorMessage = "Sorry, we were unable to log you in. Try again in a bit.";
            });
          })
          .catch((response) => {
            this.error = true;
            this.errorMessage = "Sorry, we were unable to verify your email. Try again in a bit.";
          })
          .finally(() => {
            this.isValidating = false;
          });
      }
    }

    login() {
      this.coordinator.redirectToLogin();
    }

    goWait() {
      this.coordinator.redirectToIntro();
    }

  }
}

angular.module('rambler').controller('VerifyCtrl', Rambler.VerifyCtrl);
