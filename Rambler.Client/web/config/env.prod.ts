namespace Config {
  var apiUrl = '//chatsock.example.com/api';
  var socketUrl = 'wss://chatsock.example.com/sock';

  var html5Mode: boolean = true;

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
        icon: 'fa fa-google'
      },
      {
        url: apiUrl + '/account/login?provider=Facebook',
        label: "Sign in with Facebook",
        icon: 'fa fa-facebook'
      }],
  };
}
