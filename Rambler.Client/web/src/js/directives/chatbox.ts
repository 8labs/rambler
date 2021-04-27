angular.module('rambler').directive("chatBox",
  ['$timeout', '$anchorScroll', '$window', 'ChatStateService', function ($timeout: ng.ITimeoutService, $anchorScroll: ng.IAnchorScrollService, $window: any, chat: Rambler.ChatStateService) {
    return {
      templateUrl: 'views/chatbox.html',
      transclude: true,
      scope: {
        messages: "=",
        selectedUser: "=",
        ignores: "=",
      },
      link: function (scope, element: any, attr) {

        const ele = element[0] as HTMLElement;

        let preventScroll: boolean = false;

        // When someone scrolls, if they've scrolled up, prevent auto scroll.
        // If they scroll back to the bottom, re-enable auto scroll.
        ele.onscroll = () => {
          $timeout(function () {
            if ((ele.scrollTop + ele.offsetHeight + 10) >= ele.scrollHeight) {

              (scope as any).newMessages = false;
              preventScroll = false;
              (scope as any).newMessages = false;

            } else {
              preventScroll = true;
            }
          });
        }

        var scrolly = (force: boolean = false) => {
          if (force || !preventScroll) {
            // only scroll if already at the bottom
            $timeout(function () {
              ele.scrollTop = ele.scrollHeight;
              (scope as any).newMessages = false;
            }, 1);
          }
        };

        angular.element($window).bind('resize', () => {
          scrolly();
        });

        (scope as any).scrollToBottom = () => {
          scrolly(true);
        }

        (scope as any).getUser = (id: string) => chat.state.getUser(id);
        (scope as any).userInRoom = (id: string) => chat.state.userInRoom(id);

        // (scope as any).getYouTubeLink = (message: string) => {
        //   if (message.indexOf("youtu.be") > -1) {
        //     const youtubeid = /https?:\/\/youtu.be\/(\w+)/gi.exec(message);
        //     if (youtubeid) {
        //       return youtubeid;
        //     }
        //   }

        //   return undefined;
        // }

        (scope as any).filterIgnores = (msg: Rambler.Response<Rambler.IChannelResponse>) => {
          return (!(scope as any).ignores[msg.Data.UserId]);
        }

        scope.$watchCollection('messages', (newValue, oldValue) => {
          // auto scroll any time messages are changed
          scrolly();
          if (preventScroll) {
            (scope as any).newMessages = true;
          }
        });

        scope.$watch('messages', (newValue, oldValue) => {
          // auto scroll any time messages are changed
          scrolly(true);
        });

      }
    }
  }]);
