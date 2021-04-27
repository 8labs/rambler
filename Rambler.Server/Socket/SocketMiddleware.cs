namespace Rambler.Server.Socket
{
    using Contracts.Server;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    public class SocketMiddleware
    {
        readonly SocketSubscriptions subscriptions;
        readonly ISocketSubscriber subscriber;
        readonly ILogger log;
        readonly IRequestPublisher reqPub;
        readonly IAuthorize authorize;
        readonly DnsBlackListService blacklist;

        //tracking how long a client was connected
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        public static int RateLimit = 5; //messages per second maximum

        public SocketMiddleware(
            SocketSubscriptions subscriptions,
            ISocketSubscriber subscriber,
            IRequestPublisher reqPub,
            IAuthorize authorize,
            DnsBlackListService blacklist,
            ILogger<SocketMiddleware> log)
        {
            this.subscriptions = subscriptions;
            this.subscriber = subscriber;
            this.log = log;
            this.reqPub = reqPub;
            this.authorize = authorize;
            this.blacklist = blacklist;
        }

        public void Use(IApplicationBuilder app)
        {
            app.Use((ctx, next) =>
            {
                if (ctx.WebSockets.IsWebSocketRequest)
                {
                    return Acceptor(ctx);
                }
                else
                {
                    return next();
                }
            });
        }

        public async Task Acceptor(HttpContext ctx)
        {
            var ip = ctx.GetRealIpAddress();
            var ipAddress = ip.ToString();

            log.LogInformation("Accepting connection from {IP}", ipAddress);
            var startTimestamp = Stopwatch.GetTimestamp();
            using (var socket = await ctx.WebSockets.AcceptWebSocketAsync())
            {
                // gets the user information from the connecting client
                var token = GetTokenString(ctx.Request.Query);
                // note: we're going to do this in the state
                var id = authorize.Authorize(token, true);

                if (id != null)
                {
                    if (await blacklist.IsIpBlacklisted(ip))
                    {
                        await socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "Sorry! You're on a blacklist. If you're on a VPN, please connect without it and we promise to respect your privacy.", ctx.RequestAborted);
                        return;  //BOOP
                    }

                    var handler = new SocketHandler(ctx, socket, log, CancellationToken.None, Guid.NewGuid(), id);

                    log.LogDebug("Subscribing {IP}, socket {socket}, to user {user}", ipAddress, handler.Id, id.Nick);

                    handler.Subscribe(handler.Id);  // this socket's ID
                    handler.Subscribe(id.UserId);   // the userid from the token

                    if (subscriptions.Add(handler))
                    {
                        //this loop will remain open for the lifetime of the socket/request
                        try
                        {
                            await handler.Read(subscriber, reqPub, RateLimit, token, ipAddress);
                        }
                        catch (WebSocketException ex)
                        {
                            // these fire when the underlying socket gets closed before the websocket stuff handles it
                            // (not unusual behavior)
                            log.LogInformation("Error while reading websocket: {msg}", ex.Message);
                            log.LogDebug(0, ex, "WebSocketException");
                        }
                        catch (Exception ex)
                        {
                            // these fire when the underlying socket gets closed before the websocket stuff handles it
                            log.LogError(0, ex, "Unexpected error in websocket loop");
                        }
                    }
                    subscriptions.Remove(handler);
                }
                else
                {
                    log.LogDebug("Invalid Identity token from {IP}", ipAddress);
                    try
                    {
                        await socket.CloseOutputAsync(WebSocketCloseStatus.InvalidPayloadData, "Invalid Token", ctx.RequestAborted);
                    }
                    catch (Exception ex)
                    {
                        // for some reason this usually throws an exception
                        // appears to be underlying socket madness (null reference error from deep in websocket framework)
                        log.LogError(0, ex, "Unexpected error disconnecting socket for bad token");
                    }
                }
            }

            //calculating request time the same way as the aspnetcore host logger does it
            var stopTimestamp = Stopwatch.GetTimestamp();
            var elapsed = new TimeSpan((long)(TimestampToTicks * (stopTimestamp - startTimestamp)));
            log.LogInformation("Disconnecting {IP} after {elapsedMs}ms", ipAddress, elapsed.TotalMilliseconds);
        }

        private static string GetTokenString(IQueryCollection query)
        {
            if (query.ContainsKey("token"))
            {
                //get the ID from the URL token
                return query["token"];
            }

            return null;
        }
    }
}
