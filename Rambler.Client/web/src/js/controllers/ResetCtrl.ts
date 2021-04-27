namespace Rambler {
  export class ResetCtrl {
    private isValidating: boolean = true;
    private error: boolean;
    private errorMessage: string;
    private resetForm: ng.IFormController;
    private resetRequest: Rambler.PasswordReset = {} as Rambler.PasswordReset;

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
      this.resetRequest.Email = this.routeParams.email;
      this.resetRequest.Token = this.routeParams.code;
    }

    reset() {
      if (this.routeParams.code && this.routeParams.email) {
        this.isValidating = true;

        this.analyticsService.event('ResetVerification started', this.routeParams.email, '');

        this.api
          .verifyResetPassword(this.resetRequest)
          .then((response) => {
            this.error = false;
            this.api
              .getChatToken()
              .then((response) => {
                this.coordinator.clear();
                this.coordinator.setToken(response);
                this.coordinator.redirectToChat();
              })
              .catch((reason) => {
                this.analyticsService.event('ResetVerification Failed', this.routeParams.email, '');
                this.error = true;
                this.errorMessage = 'Sorry, we were unable to log you in. Try again in a bit.';
              });
          })
          .catch((response) => {
            this.error = true;
            this.errorMessage = 'Sorry, we were unable to verify your email. Try again in a bit.';
          })
          .finally(() => {
            this.isValidating = false;
          });
      }
    }

    login() {
      this.coordinator.redirectToLogin();
    }

    onCaptcha(token: string) {
      this.resetRequest.Captcha = token;
    }

    goWait() {
      this.coordinator.redirectToIntro();
    }
  }
}

angular.module('rambler').controller('ResetCtrl', Rambler.ResetCtrl);
