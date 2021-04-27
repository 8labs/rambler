namespace Rambler.Contracts.Requests
{
    [MessageKey(KEY)]
    public class AuthRequest
    {
        public const string KEY = "AUTH";

        public string Token { get; set; }
    }
}
