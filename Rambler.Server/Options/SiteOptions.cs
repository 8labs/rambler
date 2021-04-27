namespace Rambler.Server.Options
{
    public class SiteOptions
    {
        public string AppUrl { get; set; }

        public string CookieDomain { get; set; }

        public string CookieIdentity { get; set; }

        public bool AllowGuests { get; set; }

        public bool RequireVerification { get; set; }

        public bool EnableCaptcha { get; set; }
    }
}
