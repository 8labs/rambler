namespace Rambler.Server.WebService.Services
{
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class CaptchaService : ICaptchaService
    {
        private readonly string secret = "6LdoHloUAAAAAKE-zKj9AI0_UeIE5hbo8NtdLIJD";
        private readonly string serviceUrl = "https://www.google.com/recaptcha/api/siteverify";

        private readonly ILogger<CaptchaService> log;

        public CaptchaService(ILogger<CaptchaService> log)
        {
            this.log = log;
        }

        public async Task<CaptchaResults> Validate(string captchaResponse, string remoteIp = null)
        {
            try
            {
                return await SiteVerify(captchaResponse, remoteIp);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to validate captcha. {message}", ex.Message);
                // we're going to ignore these 
                // this means the service itself is 'broken'
                return new CaptchaResults() { success = true };
            }
        }

        private async Task<CaptchaResults> SiteVerify(string captchaResponse, string remoteIp = null)
        {
            using (var client = new HttpClient())
            {
                var data = new Dictionary<string, string>()
                {
                    { "secret", secret },
                    { "response", captchaResponse },
                };

                if (!string.IsNullOrWhiteSpace(remoteIp))
                {
                    data.Add("remoteip", remoteIp);
                }

                var content = new FormUrlEncodedContent(data);
                var results = await client.PostAsync(serviceUrl, content);

                if (results.IsSuccessStatusCode)
                {
                    // bad captchas still mean results return
                    var resultContent = await results.Content.ReadAsStringAsync();
                    var responsejs = JsonConvert.DeserializeObject<CaptchaResults>(resultContent);
                    return responsejs;
                }
                else
                {
                    throw new Exception("Bad results from captcha.");
                }
            }
        }


    }
}
