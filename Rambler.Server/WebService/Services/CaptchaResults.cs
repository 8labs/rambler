namespace Rambler.Server.WebService.Services
{
    using System;

    public class CaptchaResults
    {
        //      {
        //"success": true|false,
        //"challenge_ts": timestamp,  // timestamp of the challenge load (ISO format yyyy-MM-dd'T'HH:mm:ssZZ)
        //"hostname": string,         // the hostname of the site where the reCAPTCHA was solved
        //"error-codes": [...]        // optional
        //  }
        public bool success { get; set; }
        public DateTime challenge_ts { get; set; }
        public string hostname { get; set; }
        public string[] error_codes { get; set; }
    }
}
