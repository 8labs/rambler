namespace Rambler.Server.WebService.Services
{
    using System.Threading.Tasks;

    public interface ICaptchaService
    {
        Task<CaptchaResults> Validate(string captchaResponse, string remoteIp = null);
    }
}
