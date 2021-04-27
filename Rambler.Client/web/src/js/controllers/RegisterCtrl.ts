namespace Rambler {
  export class RegisterCtrl {
    registrationRequest: Rambler.Register = {} as Rambler.Register;
    private registrationForm: ng.IFormController;
    public registering: boolean = false;
    public error: boolean = false;
    public errorMessage: string;
    public registered: boolean = false;

    static $inject = ['$location', 'RamblerApiService', 'AnalyticsService', 'CoordinationService'];

    constructor(
      private location: ng.ILocationService,
      public api: RamblerApiService,
      private analytics: AnalyticsService,
      private coordinator: CoordinationService
    ) {
      this.analytics.event('RegisterLoaded', '', '');
    }

    createAccount() {
      if (this.registrationRequest.Nick.match(Config.environment.badwords)) {
        return;
      }
      this.analytics.event('RegistrationRequested', this.registrationRequest.Nick, '');
      this.registering = true;
      this.api
        .register(this.registrationRequest)
        .then((response) => {
          this.analytics.event('RegistrationSuccessful', this.registrationRequest.Nick, '');
          this.registered = true;
        })
        .catch((reason) => {
          this.analytics.event('RegistrationFailed', this.registrationRequest.Nick, '');

          this.error = true;

          if (reason.status === 400 && reason.data) {
            this.errorMessage = reason.data;
          } else {
            this.errorMessage = 'Sorry, we were unable to register. Try again in a bit.';
          }
        }).finally(() => {
          this.registering = false;
        });
    }

    loginSocial(provider: string) {
      this.api.loginSocial(provider);
    }

    login() {
      this.coordinator.redirectToLogin();
      this.location.path('/login');
    }

    clearErrors() {
      this.error = false;
      delete this.errorMessage;
    }

    onCaptcha(token: string) {
      this.registrationRequest.Captcha = token;
    }

    goWait() {
      this.coordinator.redirectToIntro();
    }
  }
}

angular.module('rambler').controller('RegisterCtrl', Rambler.RegisterCtrl);
