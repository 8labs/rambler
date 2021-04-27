namespace Rambler.Server.Socket
{
    using Microsoft.AspNetCore.Http;
    using System.Linq;
    using System.Net;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http.Features;


    public static class HttpContextExtensions
    {
        public const string X_REAL_IP_HEADER = "X-Real-IP";

        public static IPAddress GetRealIpAddress(this HttpContext ctx)
        {
            if (ctx.Request.Headers.TryGetValue(X_REAL_IP_HEADER, out var ip) && ip.Count > 0)
            {
                if (IPAddress.TryParse(ip.First(), out var address))
                {
                    return address;
                }
            }

            return ctx.Request.HttpContext.Connection.RemoteIpAddress;
        }

        public static void SetResponseStatus(this HttpContext ctx, int code, string reason)
        {
            ctx.Response.StatusCode = code;
            ctx.Response.HttpContext
               .Features
               .Get<IHttpResponseFeature>()
               .ReasonPhrase = reason;
        }
    }
}
