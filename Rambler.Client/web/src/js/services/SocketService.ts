namespace Rambler {

  export class Sender {

    constructor(private ws: WebSocket) { }

    public directMessage(msgId: string, message: string) {
      this.send<Partial<DirectMessageRequest>>("DM", { UserId: msgId, Message: message });
    }

    public joinById(chanId: string) {
      this.send<Partial<JoinRequest>>("JOIN", { ChannelId: chanId });
    }

    public joinByName(name: string) {
      this.send<Partial<JoinRequest>>("JOIN", { ChannelName: name });
    }

    public partById(chanId: string) {
      this.send<Partial<ChannelPartRequest>>("CHPART", { ChannelId: chanId });
    }

    public chanMsg(chanId: string, msg: string) {
      this.send<ChannelMessageRequest>("CHMSG", { ChannelId: chanId, Message: msg });
    }

    public chanUsers(chanId: string) {
      this.send<ChannelUsersRequest>("CHUSERS", { ChannelId: chanId });
    }

    public send<T>(key: string, msg: T) {
      console.log('SEND:' + key, msg);
      this.ws.send(key + JSON.stringify(msg));
    }
  }

  export class SocketService {

    public static AUTH: string = 'AUTH';
    public static JOIN: string = 'JOIN';
    public static CHUSERS: string = 'CHUSERS';
    public static CHUPDATE: string = 'CHUPDATE';
    public static CHUSERUPDATE: string = 'CHUSERUP';
    public static CHJOIN: string = 'CHJOIN';
    public static CHPART: string = 'CHPART';
    public static CHMSG: string = 'CHMSG';
    public static CHBAN: string = 'CHBAN';
    public static CHREBUILD: string = 'CHREBUILD';
    public static ERROR: string = 'ERROR';
    public static DM: string = 'DM';

    private ws: WebSocket;

    public onerror: (e: Event) => void;
    public onopen: (e: Event) => void;
    public onclose: (e: CloseEvent) => void;
    public onmessage: (e: MessageEvent) => void;
    public onresponse: (msg: IResponse) => void;

    public sender: Sender;

    static $inject = ['$rootScope', '$log'];
    constructor(private scope: ng.IRootScopeService, private log: ng.ILogService) { }

    public disconnect() {
      this.ws && this.ws.close();
    }

    public connect(token: string) {
      var u = Config.environment.socketUrl + "?token=" + token;
      this.ws = new WebSocket(u);

      this.ws.onclose = (e) => {
        this.scope.$applyAsync();
        this.onclose(e);
      }

      this.ws.onerror = (e) => {
        this.scope.$applyAsync();
        this.onerror(e);
      }

      this.ws.onopen = (e) => {
        this.scope.$applyAsync();
        this.onopen(e);
      }

      this.sender = new Sender(this.ws);

      this.ws.onmessage = (e) => {
        this.onmessage && this.onmessage(e);

        const msg = JSON.parse(<string>e.data);

        this.log.info(msg);

        this.onresponse && this.onresponse(msg);

        // TODO: figure out if there's a better (more performant) way of handling this
        // might end up using something else to handle the main chat window anyway
        // this might also make more sense in the chat state service (since not all messages trigger a state update)
        this.scope.$applyAsync();
      };
    }

    public getState(): number {
      return this.ws.readyState;
    }
  }
}

angular
  .module('rambler')
  .service('SocketService', Rambler.SocketService)
