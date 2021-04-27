namespace Rambler {
  export class UserListCtrl {
    public users: IUser[];
    static $inject = ['ChatStateService'];
    constructor(public chat: ChatStateService) {
    }
  }
}

angular.module('rambler').component('userList', {
  templateUrl: 'views/userlist.html',
  bindings: {
    users: '<'
  },
  controller: Rambler.UserListCtrl
});
