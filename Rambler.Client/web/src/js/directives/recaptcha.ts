declare var grecaptcha: any;

angular.module('rambler').directive('recaptcha', [
  '$parse',
  '$timeout',
  function($parse: ng.IParseService, $timeout: ng.ITimeoutService) {
    return function(scope, element, attrs) {
      const fn = $parse(attrs.recaptcha);

      grecaptcha.render(element[0], {
        sitekey: '6LdoHloUAAAAAMsVLeEs_sH8sHa272jrP4ZMv2Dx',
        callback: (t: string) => {
          $timeout(() => {
            fn(scope, { $token: t });
          });
        },
      });
    };
  },
]);
