angular.module('rambler').filter('profanity', function () {
  return function (msg: string) {
    if (!msg) return msg;
    return msg.replace(Config.environment.badwords, (match: any) => {
      return match.replace(/\w+/, '*bleep*');
    });
  };
});
