namespace Rambler.Server.Socket
{
    using Contracts.Requests;
    using Contracts.Server;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts.Api;

    public class SocketHandler
    {
        public int ReadBufferSize { get; set; } = 1024 * 4;
        public int MaxBufferedBytes { get; set; } = 1024 * 4 * 2;

        /// <summary>
        /// track this socket's subscriptions so they can be removed on close
        /// </summary>
        private readonly ConcurrentDictionary<Guid, object> subscriptions;

        public IEnumerable<Guid> Subscriptions
        {
            get { return subscriptions.Keys; }
        }

        /// <summary>
        /// This socket's unique ID
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// This socket's user Identity
        /// </summary>
        public IdentityToken Identity { get; }

        HttpContext context;
        WebSocket socket;
        ILogger logger;
        CancellationToken token;
        bool isClosing = false; //force dump on socket send errors

        public SocketHandler(HttpContext context, WebSocket socket, ILogger logger, CancellationToken token, Guid id, IdentityToken identity)
        {
            subscriptions = new ConcurrentDictionary<Guid, object>();
            this.context = context;
            this.socket = socket;
            this.logger = logger;
            this.token = token;
            Id = id;
            Identity = identity;
        }

        /// <summary>
        /// Checks to see if this socke has the specificed subscription
        /// </summary>
        /// <param name="sub"></param>
        /// <returns></returns>
        public bool IsSubscribed(Guid sub)
        {
            return subscriptions.ContainsKey(sub);
        }

        /// <summary>
        /// Adds the specified subscription to the socket
        /// </summary>
        /// <param name="sub"></param>
        /// <returns>True if new subscription.  False if add fails</returns>
        public bool Subscribe(Guid sub)
        {
            return subscriptions.TryAdd(sub, null);
        }

        public bool UnSubscribe(Guid sub)
        {
            return subscriptions.TryRemove(sub, out var trash);
        }

        public async Task Publish(ArraySegment<byte> message, WebSocketMessageType dataType)
        {
            try
            {
                //can skip some exceptions by checking this first
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(message, dataType, true, token);
                }
            }
            catch (Exception ex)
            {
                //chances are this is just the socket closing
                logger.LogWarning(0, ex, "Error sending to socket");

                //the socket should already be cleaning itself up
                //adding this to break the read loop, just in case.
                isClosing = true;
            }
        }

        public async Task Disconnect(WebSocketCloseStatus status, string closeMessage)
        {
            try
            {
                await socket.CloseOutputAsync(status, closeMessage, token);
            }
            catch (Exception ex)
            {
                //chances are this is just the socket closing
                logger.LogWarning(0, ex, "Error disconnecting socket");
            }
            isClosing = true;
        }

        /// <summary>
        /// Handles reading data from the socket
        /// Buffering, multiple frames, closing, etc.
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public async Task Read(ISocketSubscriber subscriber, IRequestPublisher publisher, int rateLimit, string identityToken, string ipAddress)
        {
            var buffer = new byte[ReadBufferSize];
            var seg = new ArraySegment<byte>(buffer);

            const int per = 1000; //unit: ms (messages/ms);
            double allowance = rateLimit;
            var lastCheck = DateTime.UtcNow;

            try
            {
                //immediately send the auth
                await publisher.Publish(new Request<AuthRequest>()
                {
                    UserId = Identity.UserId,
                    SocketId = Id,
                    IPAddress = ipAddress,
                    Data = new AuthRequest()
                    {
                        Token = identityToken,
                    }
                });

                using (var stream = new MemoryStream(MaxBufferedBytes))
                {
                    while (socket.State == WebSocketState.Open && !isClosing)
                    {
                        var result = await socket.ReceiveAsync(seg, token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            logger.LogDebug("Socket closed: {status} {description}.", socket.CloseStatus, socket.CloseStatusDescription);
                            break;
                        }
                        //check for going over max buffered size
                        else if (stream.Length > ReadBufferSize)
                        {
                            logger.LogInformation("Disconnecting socket for too large of message.");
                            await socket.CloseOutputAsync(WebSocketCloseStatus.MessageTooBig, "You sent too much crap.", token);
                            break;
                        }

                        //write the the incoming data
                        stream.Write(buffer, 0, result.Count);

                        if (result.EndOfMessage)
                        {
                            var current = DateTime.UtcNow;
                            var passed = current - lastCheck;
                            lastCheck = current;
                            allowance += passed.TotalMilliseconds * (rateLimit / (double)per);
                            if (allowance > rateLimit) allowance = rateLimit; //throttle

                            if (allowance < 1)
                            {
                                //discard
                                //TODO - this maybe should boot them instead.
                                //the client should handle warning users if they type too fast
                                //this limiter should only be necessary for people trying to bypass the client.
                                //could also track over allowance messages and only boot after awhile
                                //that might keep spammers from figuring out the actual rate limit
                            }
                            else
                            {
                                allowance -= 1;
                                //distrute the data

                                await subscriber.OnMessageReceived(
                                    Id,
                                    Identity.UserId,
                                    ipAddress,
                                    stream,
                                    result.MessageType);
                            }

                            //reset the stream
                            stream.Position = 0;
                            stream.SetLength(0);
                        }
                        else
                        {
                            logger.LogDebug("Not end of message");
                        }
                    }
                }
            }
            finally
            {
                //publish disconnect action on exit for whatever reason
                await publisher.Publish(new Request<DisconnectRequest>()
                {
                    UserId = Identity.UserId,
                    SocketId = Id,
                    IPAddress = ipAddress,
                    Data = new DisconnectRequest(),
                });
            }
        }
    }


}
