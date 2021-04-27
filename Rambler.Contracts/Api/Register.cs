namespace Rambler.Contracts.Api
{
    /// <summary>
    /// Register model
    /// </summary>
    public class Register
    {
        public string Nick { get; set; }
        public string Email { get; set; }
        public string PasswordVerify { get; set; }
        public string Password { get; set; }
        public string Captcha { get; set; }
    }
}
