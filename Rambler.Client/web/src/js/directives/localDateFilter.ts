angular.module('rambler').filter('localDateFilter', function () {
  // In the return function, we must pass in a single parameter which will be the data we will work on.
  // We have the ability to support multiple other parameters that can be passed into the filter optionally
  return function (input: string) {
    var d = new Date(input);
    return d.toLocaleDateString();
  }
});
