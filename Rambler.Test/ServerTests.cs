namespace Rambler.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Contracts.Api;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Server;
    using Contracts.Requests;
    using Contracts.Responses;
    using Contracts.Server;
    using Server.Socket;
    using Server.State;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net;
    using Rambler.Server.Database.Models;

    [TestClass]
    public class ServerTests
    {
        //    public class FakePublisher : IPublisher
        //    {
        //        private readonly ILogger log;
        //        public FakePublisher(ILogger<FakePublisher> log)
        //        {
        //            this.log = log;
        //        }
        //        public Task Publish<T>(Response<T> response, IEnumerable<SocketHandler> subscribers)
        //        {
        //            var message = JsonConvert.SerializeObject(response);
        //            log.LogDebug(message);
        //            return Task.CompletedTask;
        //        }
        //    }

        //    private class TestServer
        //    {
        //        public IServiceProvider Provider { get; set; }

        //        public ISocketSubscriber SocketSubscriber { get; set; }
        //        public StateCache Cache { get; set; }
        //        public StateServer StateServer { get; set; }
        //        public StateMutator StateMutator { get; set; }
        //        public NodeServer NodeServer { get; set; }
        //        public SocketSubscriptions Subscriptions { get; set; }
        //        public IRequestPublisher RequestPublisher { get; set; }

        //        public TestServer()
        //        {
        //            var services = new ServiceCollection()
        //                .AddLogging();

        //            var startup = new Startup(null);
        //            startup.ConfigureServices(services);

        //            services.AddSingleton<IPublisher, FakePublisher>(); //override

        //            Provider = services.BuildServiceProvider(true);

        //            var loggerFactory = Provider.GetService<ILoggerFactory>();
        //            loggerFactory.AddDebug(LogLevel.Debug);

        //            startup.Boot(Provider);

        //            SocketSubscriber = Provider.GetService<ISocketSubscriber>();
        //            Cache = Provider.GetService<StateCache>();
        //            StateServer = Provider.GetService<StateServer>();
        //            NodeServer = Provider.GetService<NodeServer>();
        //            Subscriptions = Provider.GetService<SocketSubscriptions>();
        //            RequestPublisher = Provider.GetService<IRequestPublisher>();
        //            StateMutator = Provider.GetService<StateMutator>();
        //        }
        //    }

        //    [Fact]
        //    public async void Test()
        //    {
        //        var server = new TestServer();

        //        //setup some fake crap
        //        var user = Guid.NewGuid();
        //        var channel = Guid.NewGuid();
        //        server.Provider.GetService<StateCache>().AddChannelUser(channel, user);

        //        using (var stream = new MemoryStream())
        //        {
        //            var data = GetJsonBytes(ChannelMessageRequest.KEY, new ChannelMessageRequest()
        //            {
        //                Channel = channel,
        //                Message = "Test message",
        //            });
        //            await stream.WriteAsync(data, 0, data.Length);

        //            await server.SocketSubscriber.OnMessageReceived(user, user, new IPAddress(0), stream, WebSocketMessageType.Text);
        //        }

        //        await server.StateMutator.Stop();

        //        var node = server.NodeServer.Start();
        //        server.NodeServer.Shutdown();
        //        await node;
        //    }

        //    private byte[] GetJsonBytes(string key, object o)
        //    {
        //        var json = JsonConvert.SerializeObject(o);
        //        return Encoding.UTF8.GetBytes(key + json);
        //    }

        [DataTestMethod]
        [DataRow("bob", true)]
        [DataRow("33bob_33", true)]
        [DataRow("BOB_TEST", true)]
        [DataRow("เมือง", true)]

        [DataRow("Space InName", false)]
        [DataRow("Undrscr_InName", true)]
        [DataRow("Dash-InName", true)]
        [DataRow("", false)]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ", false)]
        [DataRow(" A", false)]
        public void TestNames(string name, bool valid)
        {
            Assert.AreEqual(Server.State.Processors.AuthRequestProcessor.IsValidName(name), valid, name);
        }

        [TestMethod]
        public void UserLevelsMatchModLevels()
        {
            // mode levels need some cleanup
            // tossing this in here just to catch obvious problems
            foreach (var al in Enum.GetValues(typeof(ApplicationUser.UserLevel)))
            {
                var found = false;
                foreach (var ml in Enum.GetValues(typeof(ModerationLevel)))
                {
                    if ((int)ml == (int)al) found = true;
                }
                Assert.IsTrue(found, "could not match user level for: " + al);
            }
        }

        //    [Fact]
        //    public async void TestKaboom()
        //    {
        //        var identity = new IdentityToken()
        //        {
        //            Nick = "bob",
        //            UserId = Guid.NewGuid(),
        //        };
        //        var server = new TestServer();

        //        var sw = Stopwatch.StartNew();

        //        List<Guid> channels = new List<Guid>();
        //        List<Guid> sockets = new List<Guid>();
        //        var rnd = new Random();
        //        for (var i = 0; i < 1000; i++)
        //        {
        //            channels.Add(Guid.NewGuid());
        //        }

        //        //generate x number of sockets
        //        for (var i = 0; i < 10000; i++)
        //        {
        //            var id = Guid.NewGuid();
        //            sockets.Add(id);
        //            server.Subscriptions.Add(new SocketHandler(null, null, null, System.Threading.CancellationToken.None, id, identity));
        //            server.Cache.AddSocket(id, null, identity.UserId, identity.Nick, identity.IsGuest);

        //            for (var j = 0; j < 100; j++)
        //            {
        //                server.Cache.AddChannelUser(channels[rnd.Next(1000)], id);
        //            }
        //        }

        //        //logger.LogInformation("Finished adding: " + sw.ElapsedMilliseconds);
        //        var node = server.NodeServer.Start();

        //        sw.Restart();

        //        //pump through some messages
        //        Parallel.For(0, 10000, async (x) =>
        //        {
        //            await server.RequestPublisher.Publish(new Request<JoinRequest>()
        //            {
        //                UserId = sockets[x],
        //                Data = new JoinRequest()
        //                {
        //                    ChannelId = channels[rnd.Next(1000)],
        //                }
        //            });
        //        });

        //        await server.StateMutator.Stop();

        //        server.NodeServer.Shutdown();
        //        await node;

        //        //logger.LogInformation("Done: " + sw.ElapsedMilliseconds);
        //    }

    }
}