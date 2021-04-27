namespace Rambler.Server.Socket
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Options;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public class DnsBlackListService
    {
        private readonly DnsBlackListOptions options;
        private readonly ILogger<DnsBlackListService> log;

        public DnsBlackListService(IOptions<DnsBlackListOptions> optionsAccessor, ILogger<DnsBlackListService> log)
        {
            this.log = log;
            options = optionsAccessor.Value;
        }

        public async Task<bool> IsIpBlacklisted(IPAddress ip)
        {
            foreach (var server in options.Servers)
            {
                try
                {
                    var lookup = GetLookupAddress(ip, server);
                    log.LogDebug("Testing IP {ip} against blacklist {server} : {lookup}", ip, server, lookup);
                    var results = await IsHostEntryListed(lookup);

                    if (results)
                    {
                        log.LogInformation("IP {ip} is blacklisted on {server}", ip, server);
                        return true;
                    }
                }
                catch (SocketException)
                {
                    log.LogInformation("Socket error testing {ip} against {server} ", ip, server);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Failed to lookup IP {ip} on DNSBL {server}. {message}", ip, server, ex.Message);
                }
            }

            return false;
        }

        private static IPAddress GetReversedIp(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            Array.Reverse(bytes, 0, bytes.Length);
            var rev = new IPAddress(bytes);
            return rev;
        }

        private static string GetLookupAddress(IPAddress ip, string blserver)
        {
            var rev = GetReversedIp(ip);
            var lookup = rev.ToString() + blserver;
            return lookup;
        }

        private static async Task<bool> IsHostEntryListed(string lookup)
        {
            try
            {
                var ipEntry = await Dns.GetHostEntryAsync(lookup);
                // IP address was found on the BLServer,
                // it's then listed in the black list
                return true;
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 11001)
                {
                    // IP address not listed
                    return false;
                }

                // let other errors bubble up
                throw;
            }
        }
    }
}
