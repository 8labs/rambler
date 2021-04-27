namespace Rambler.Contracts.Api
{
    /// <summary>
    /// Note:  this class is getting realllllyyy overused for a lot of the verification api objects
    /// </summary>
    public class PasswordReset
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string Captcha { get; set; }
    }
}
