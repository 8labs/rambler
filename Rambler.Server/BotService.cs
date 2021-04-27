namespace Rambler.Server
{
    using Contracts.Responses;
    using Database.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Utility;

    public class BotService
    {
        private class SubscribedBot
        {
            public SerialQueue Queue { get; }

            public IEnumerable<Guid> Channels { get; }

            public DateTime IgnoreUntil { get; set; }

            public bool IsIgnored()
            {
                return IgnoreUntil > DateTime.UtcNow;
            }

            public Bot Bot { get; }

            public SubscribedBot(Bot bot, IEnumerable<Guid> channels)
            {
                Bot = bot;
                Channels = channels;
                Queue = new SerialQueue();
            }
        }

        public const int SEND_RETRIES = 3;

        private readonly List<SubscribedBot> Bots = new List<SubscribedBot>();
        private readonly TaskQueue queue = new TaskQueue();
        private readonly ILogger log;

        public BotService(ILogger<BotService> log, IResponseDistributor distributor)
        {
            this.log = log;

            // channel messages bots can subscription to
            distributor.Subscribe<ChannelMessageResponse>(OnResponse);
            distributor.Subscribe<ChannelJoinedResponse>(OnResponse);
            distributor.Subscribe<ChannelPartResponse>(OnResponse);
            distributor.Subscribe<ChannelUserUpdateResponse>(OnResponse);
            distributor.Subscribe<ChannelUpdateResponse>(OnResponse);
        }

        public void AddBot(Bot bot, IEnumerable<Guid> channels)
        {
            Bots.Add(new SubscribedBot(bot, channels));
        }

        public Task Start()
        {
            return queue.Start();
        }

        public void Shutdown()
        {
            queue.Stop();
        }

        public Task OnResponse<T>(Response<T> response)
        {
            queue.Enqueue(() =>
            {
                var sends = Bots
                    .Where(b => !b.IsIgnored() && b.Bot.IsEnabled)
                    .Where(b => b.Channels.Contains(response.Subscription))
                    // TODO - filter out messages the bot doesn't care about
                    .Select(b => b.Queue.Enqueue(() => Send(b, response)))
                    .ToList();

                // fire and forget - let the send function handle errors, etc.
                // we could wait all the sends if we have reason to in the future
                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        }

        private async Task Send(SubscribedBot bot, IResponse response)
        {
            if (bot.IsIgnored()) return; // previous task fired the ignore

            var data = JsonConvert.SerializeObject(response);
            var content = new StringContent(data);

            for (var x = 0; x < SEND_RETRIES; x++)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var res = await client.PostAsync(bot.Bot.Name, content);
                        if (res.IsSuccessStatusCode)
                        {
                            log.LogDebug("Sent bot {name} message.", bot.Bot.Name);
                            return;  // all done
                        }
                        else
                        {
                            log.LogDebug("Failed to send bot {name} message.  Status {status}, {statusText}.",
                                bot.Bot.Name,
                                res.StatusCode,
                                res.ReasonPhrase);

                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        log.LogError(0, ex, "Error sending to bot {name}", bot.Bot.Name);
                    }
                }

                if (x < SEND_RETRIES)
                {
                    log.LogDebug("Retrying bot send.");
                    await Task.Delay(5000 * (x + 1));
                }
            }

            log.LogDebug("Bot {name} failed to send.  Shutting it down for awhile", bot.Bot.Name);
            bot.IgnoreUntil = DateTime.UtcNow.AddMinutes(15);
        }
    }
}
