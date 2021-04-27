namespace Rambler.Server.Options
{
    public class TokenOptions
    {
        public string Secret { get; set; }

        public int ExpirationMinutes { get; set; }
    }
}
