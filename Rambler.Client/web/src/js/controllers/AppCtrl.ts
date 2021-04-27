namespace Rambler {
  export class AppCtrl {
    static $inject = ['$location', '$route', '$routeParams'];
    constructor(public $location: ng.ILocationService, public $route: ng.route.IRouteService, public $routeParams: ng.route.IRouteParamsService) {
    }

  }
}

angular
  .module('rambler')
  .controller('AppCtrl', Rambler.AppCtrl);
