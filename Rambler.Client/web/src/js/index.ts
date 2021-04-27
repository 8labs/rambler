namespace Rambler {
  const app = angular.module('rambler', ['ngRoute', 'ngTouch', 'ngCookies', 'ngStorage', 'ngSanitize']);
  declare var bugsnagClient: any;

  // https://stackoverflow.com/questions/35629246/typescript-async-await-and-angular-q-service/41825004#41825004
  app.run([
    '$window',
    '$q',
    function($window: ng.IWindowService, $q: any) {
      $window.Promise = $q;
    },
  ]);

  // app.factory('$exceptionHandler', function () {
  //   return function (exception: any, cause: any) {
  //     bugsnagClient.notify(exception, {
  //       beforeSend: function (report: any) {
  //         report.updateMetaData('angular', { cause: cause })
  //       }
  //     })
  //   }
  // })

  // app.config(['$logProvider', function ($logger: ng.ILogProvider) {
  //   $logger.debugEnabled(true);
  // }]);

  app.config([
    '$httpProvider',
    function($httpProvider: ng.IHttpProvider) {
      $httpProvider.interceptors.push([
        '$q',
        '$location',
        function($q: ng.IQService, $location: ng.ILocationService) {
          return {
            response: function(response) {
              if (response.status === 401) {
                $location.path('/login');
              }
              return response || $q.when(response);
            },
            responseError: function(rejection) {
              if (rejection.status === 401) {
                $location.path('/login');
              }
              return $q.reject(rejection);
            },
          };
        },
      ]);
    },
  ]);

  app.config([
    '$routeProvider',
    '$locationProvider',
    function($routeProvider: ng.route.IRouteProvider, $locationProvider: ng.ILocationProvider) {
      $routeProvider
        .when('/chat/:nick?', {
          templateUrl: 'views/chat.html',
          controller: 'ChatCtrl',
          controllerAs: 'ctrl',
        })
        .when('/login', {
          templateUrl: 'views/login.html',
          controller: 'LoginCtrl',
          controllerAs: 'ctrl',
        })
        .when('/register', {
          templateUrl: 'views/register.html',
          controller: 'RegisterCtrl',
          controllerAs: 'ctrl',
        })
        .when('/room/:room', {
          templateUrl: 'views/direct.html',
          controller: 'IntroCtrl',
          controllerAs: 'ctrl',
        })
        .when('/embed/:room', {
          templateUrl: 'views/direct.html',
          controller: 'IntroCtrl',
          controllerAs: 'ctrl',
        })
        .when('/verify', {
          templateUrl: 'views/verify.html',
          controller: 'VerifyCtrl',
          controllerAs: 'ctrl',
        })
        .when('/reset', {
          templateUrl: 'views/reset.html',
          controller: 'ResetCtrl',
          controllerAs: 'ctrl',
        })
        .otherwise({
          templateUrl: 'views/intro.html',
          controller: 'IntroCtrl',
          controllerAs: 'ctrl',
        });

      $locationProvider.html5Mode({
        enabled: Config.environment.html5Mode,
        requireBase: false,
      });
    },
  ]);
}
