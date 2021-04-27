namespace Rambler.Server.WebService.Controllers
{
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.Google;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Authentication;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;

    [Authorize]
    public class HomeController : ControllerBase
    {
        private readonly ILogger logger;

        public HomeController(
          ILogger<AccountController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public Task Index()
        {
            //var test = await HttpContext.Authentication.GetAuthenticateInfoAsync(GoogleDefaults.AuthenticationScheme);
            // Sign the user out of the authentication middleware (i.e. it will clear the Auth cookie)
            //await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            //await HttpContext.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, )

            return Task.FromResult(0);
        }
    }
}
