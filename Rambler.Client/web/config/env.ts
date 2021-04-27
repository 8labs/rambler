// Default environment configuration (dev)
// override in files env.[env].ts
namespace Config {
  var apiUrl = '//127.0.0.1:5000/api';
  var socketUrl = 'ws://127.0.0.1:5000';

  var html5Mode: boolean = false;

  export var environment = {
    production: true,
    apiUrl: apiUrl,
    html5Mode: html5Mode,
    socketUrl: socketUrl,
    badwords: /(shit|fuck)/gi,
    logins: [
      {
        url: apiUrl + '/account/login?provider=Google',
        label: "Sign in with Google",
        icon: 'google'
      },
      {
        url: apiUrl + '/account/login?provider=Facebook',
        label: "Sign in with Facebook",
        icon: 'facebook'
      }],
  };
}
