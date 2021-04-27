namespace Rambler {
  export class LoginCtrl {

    public username: string = '';
    public password: string = '';
    public error: boolean = false;
    public errorMessage: string;
    public loginForm: ng.IFormController;
    public resetEmail: string;
    public forgotPassword: boolean = false;
    public resetSent: boolean = false;

    static $inject = ['$location', 'RamblerApiService', 'AnalyticsService', 'CoordinationService'];

    constructor(private location: ng.ILocationService, public api: RamblerApiService, private analytics: AnalyticsService, private coordinator: CoordinationService) {
      this.analytics.event('LoginLoaded', '', '');
    }

    login() {
      this.analytics.event('LoginRequested', this.username, '');
      this.api.login(this.username, this.password).then((response) => {
        this.analytics.event('LoginSuccessful', this.username, '');
        this.api.getChatToken()
          .then(response => {
            this.coordinator.clear();
            this.coordinator.setToken(response);
            this.coordinator.redirectToChat();
          });
      }).catch((reason: any) => {
        console.log(reason);
        this.analytics.event('LoginFailed', this.username, '');
        this.error = true;
        if (reason.status === 401) {
          this.errorMessage = "Sorry, that username and password combination didn't work. Maybe you need to register?";
        } else {
          this.errorMessage = "Sorry, we were unable to log you in. Try again in a bit.";
        }
      }).finally(() => {
        delete this.password;
        if (this.loginForm) {
          this.loginForm.pass.$touched = false;
        }
      });
    }

    loginSocial(provider: string) {
      this.api.loginSocial(provider);
    }

    createAccount() {
      this.location.path('/register');
    }

    resetPassword() {
      this.api.resetPassword(this.resetEmail).then((response) => {
        this.resetSent = true;
        this.forgotPassword = false;
        delete this.resetEmail;
      }).catch((response) => {
        this.errorMessage = "There was a problem resetting your password. Please try again later.";
        this.error = true;
      });
    }

    clearErrors() {
      this.error = false;
      delete this.errorMessage;
    }

  }
}

angular
  .module('rambler')
  .controller('LoginCtrl', Rambler.LoginCtrl);

