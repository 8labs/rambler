namespace Rambler.Server.WebService.Controllers
{
    using Contracts.Api;
    using Database;
    using Database.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Options;
    using System;
    using System.Linq;
    using System.Net;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using WebService.Services;

    [Authorize]
    public class AccountController : ControllerBase
    {
        public const int GUEST_TOKEN_EXPIRATION = 365;
        public const int USER_TOKEN_EXPIRATION = 1;
        private readonly ILogger logger;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly SiteOptions siteConfig;
        private readonly Server.Options.TokenOptions tokenConfig;
        private readonly Socket.IAuthorize authorizor;
        private readonly ApplicationDbContext db;
        private readonly EmailService emailSender;
        private readonly ICaptchaService captcha;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<SiteOptions> siteConfig,
            IOptions<Server.Options.TokenOptions> tokenConfig,
            Socket.IAuthorize authorizor,
            ApplicationDbContext db,
            EmailService emailSender,
            ICaptchaService captcha,
            ILogger<AccountController> logger)
        {
            this.logger = logger;
            this.siteConfig = siteConfig.Value;
            this.tokenConfig = tokenConfig.Value;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.authorizor = authorizor;
            this.db = db;
            this.emailSender = emailSender;
            this.captcha = captcha;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string provider, string returnUrl = null)
        {
            // check if it's already logged in
            // if so, redirect directly to the external login callback
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info != null)
            {
                return RedirectToAction(nameof(ExternalLoginCallback));
            }

            if (signInManager.IsSignedIn(User))
            {
                return RedirectToAction(nameof(ExternalLoginCallback));
            }

            //validate our scheme
            var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
            var scheme = schemes
                .Where(p => p.Name == provider)
                .FirstOrDefault();

            if (scheme == null)
            {
                return NotFound();
            }

            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(scheme.Name, "/api/account/ExternalLoginCallback");
            return Challenge(properties, scheme.Name);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task Logout()
        {
            // Sign the user out of the authentication middleware (i.e. it will clear the Auth cookie)
            await signInManager.SignOutAsync();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] Register registration)
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return Unauthorized();
            }

            var user = new ApplicationUser()
            {
                UserName = registration.Nick,
                Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                LastSeenDate = DateTime.UtcNow,
                RegistrationDate = DateTime.UtcNow,
            };

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return BadRequest(ModelState);
            }

            result = await userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return BadRequest(ModelState);
            }

            await signInManager.SignInAsync(user, isPersistent: false);
            logger.LogInformation(6, "User created an account using {Name} provider.", info.LoginProvider);
            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterUser([FromBody] Register registration)
        {
            if (registration.Nick.ToLower().StartsWith("guest"))
            {
                return BadRequest("Invalid username.");
            }

            if (registration.Password != registration.PasswordVerify)
            {
                return BadRequest("Passwords don't match.");
            }

            var user = new ApplicationUser()
            {
                UserName = registration.Nick,
                Email = registration.Email,
            };


            if (siteConfig.EnableCaptcha)
            {
                var res = await captcha.Validate(registration.Captcha);
                if (!res.success)
                {
                    return BadRequest("Captcha Failure " + res.error_codes);
                }
            }

            var result = await userManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                AddErrors(result);
                return BadRequest(ModelState);
            }

            await GenerateValidateEmail(user);

            await db.SaveChangesAsync();

            logger.LogInformation(6, "User created an account using password.");
            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginUser([FromBody] Auth auth)
        {
            if (string.IsNullOrWhiteSpace(auth.Username) || string.IsNullOrWhiteSpace(auth.Password))
            {
                return Unauthorized();
            }

            var user = auth.Username.Contains('@')
                ? await userManager.FindByEmailAsync(auth.Username)
                : await userManager.FindByNameAsync(auth.Username);

            if (user == null)
            {
                return Unauthorized();
            }

            if (!await signInManager.CanSignInAsync(user))
            {
                return Unauthorized();
            }

            //Add this to check if the email was confirmed.
            if (!await userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized();
            }

            var result = await signInManager.PasswordSignInAsync(user, auth.Password, true, false);

            user.LastSeenDate = DateTime.UtcNow;

            await db.SaveChangesAsync();

            if (!result.Succeeded)
            {
                return Unauthorized();
            }

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordReset reset)
        {
            var user = await userManager.FindByEmailAsync(reset.Email);

            if (user == null)
            {
                // no feedback = security apparently
                return Ok();
            }

            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            code = WebUtility.UrlEncode(code);
            var email = WebUtility.UrlEncode(user.Email);
            var link = $"{siteConfig.AppUrl}/reset/?code={code}&email={email}";

            var body = $"<div style=\"padding-bottom: 4rem;\"><center><div style=\"background-color: #5C3F70; text-align: center; padding: 4rem 0;\"><img src=\"{siteConfig.AppUrl}/assets/images/mailicon.png\" style=\"width: 75px;\" /></div></center><h3 style=\"text-align: center; font-family: sans-serif; font-weight: 200;\">Click the link to reset your password!</h3><center><a style=\"text-align: center; font-family: sans-serif; font-weight: 200; font-size: 0.9rem; margin: 2rem; padding: 0.5rem 2rem; line-height: 0.9rem; min-height: 2rem; border-radius: 2rem; background-color: #FF8CAE; color: #FFFFFF; text-decoration: none;\" href=\"{link}\">Reset your password and Chat Now!</a></center></div>";

            await emailSender.SendEmailAsync(
               user.Email,
               "Reset your password!",
               body,
               isBodyHtml: true);

            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordReset reset)
        {
            var user = await GetUser(reset.Email);
            if (user == null)
            {
                return BadRequest();
            }

            if (siteConfig.EnableCaptcha)
            {
                var cres = await captcha.Validate(reset.Captcha);
                if (cres == null || !cres.success)
                {
                    return BadRequest("invalid captcha");
                }
            }

            var res = await userManager.ResetPasswordAsync(user, reset.Token, reset.NewPassword);
            if (!res.Succeeded)
            {
                return BadRequest(res.Errors);
            }

            // Sign them in.
            await signInManager.SignInAsync(user, isPersistent: true);

            // we only allow password reset tokens via email
            user.EmailConfirmed = true;

            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RequestEmailVerification([FromBody] PasswordReset reset)
        {
            var user = await GetUser(reset.Email);

            if (user == null)
            {
                return Ok();
            }

            await GenerateValidateEmail(user);

            await db.SaveChangesAsync();

            return Ok();
        }

        private async Task GenerateValidateEmail(ApplicationUser user)
        {
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);

            code = WebUtility.UrlEncode(code);
            var email = WebUtility.UrlEncode(user.Email);
            var link = $"{siteConfig.AppUrl}/verify/?code={code}&email={email}";

            // TODO: Figure out which base url this should use from the build config.
            await emailSender.SendEmailAsync(
                user.Email,
                "Hey, " + user.UserName + "! Could you please verify your email?",
                $"<div style=\"padding-bottom: 4rem;\"><center><div style=\"background-color: #5C3F70; text-align: center; padding: 4rem 0;\"><img src=\"{siteConfig.AppUrl}/assets/images/mailicon.png\" style=\"width: 75px;\" /></div></center><h3 style=\"text-align: center; font-family: sans-serif; font-weight: 200;\">Verify your email address</h3><p style=\"text-align: center; font-family: sans-serif; font-weight: 200;\">We really don't like asking you for this. We value your privacy and won't sell or give these details to anyone else.</p><p style=\"text-align: center; font-family: sans-serif; font-weight: 200; margin-bottom: 2rem;\">But in order to keep the chat safe for everyone, we do need you to verify your email address before you can join in.</p><center><a style=\"text-align: center; font-family: sans-serif; font-weight: 200; font-size: 0.9rem; margin: 2rem; padding: 0.5rem 2rem; line-height: 0.9rem; min-height: 2rem; border-radius: 2rem; background-color: #FF8CAE; color: #FFFFFF; text-decoration: none;\" href=\"{link}\">Verify and Chat Now!</a></center></div>",
                isBodyHtml: true);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateEmail([FromBody] PasswordReset reset)
        {
            var user = await userManager.FindByEmailAsync(reset.Email);

            if (user == null)
            {
                return BadRequest();
            }

            var res = await userManager.ConfirmEmailAsync(user, reset.Token);

            if (!res.Succeeded)
            {
                return BadRequest(res.Errors);
            }

            // Sign them in.
            await signInManager.SignInAsync(user, isPersistent: true);

            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RequestEmailChange(string newEmail)
        {
            // require the user to be logged into even generate the code
            var user = await userManager.GetUserAsync(User);

            var existingUser = await userManager.FindByEmailAsync(newEmail);

            if (existingUser != null)
            {
                return BadRequest("email in use");
            }

            var code = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);

            await emailSender.SendEmailAsync(
                newEmail,
                "Verify your email!",
                 $"<a href=\"{siteConfig.AppUrl}/verify/?code={WebUtility.UrlEncode(code)}&email={WebUtility.UrlEncode(user.Email)}\">Verify now!</a>",
                isBodyHtml: true);

            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> ValidateChangeEmail([FromBody] PasswordReset reset)
        {
            // user must be logged in to change email even with token
            // (don't allow an easy hijack)
            var user = await userManager.GetUserAsync(User);

            // in case someone else grabbed it?
            var existingUser = await userManager.FindByEmailAsync(reset.Email);
            if (existingUser != null)
            {
                return BadRequest("email in use");
            }

            var res = await userManager.ChangeEmailAsync(user, reset.Email, reset.Token);
            if (!res.Succeeded)
            {
                return BadRequest(res.Errors);
            }

            await db.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] string token)
        {
            if (User.Identity.IsAuthenticated)
            {
                // non-guests we use auth to generate new tokens
                // we can just ignore incoming tokens in this case and send them a new one
                var user = await userManager.GetUserAsync(User);
                return Ok(CreateTokenForUser(user));
            }

            // grab the ID, but don't validate if it's expired
            // especially for guests, we care that the ID is correct
            var id = authorizor.Authorize(token, false);
            if (id == null)
            {
                // invalid data = don't trust this user
                return Unauthorized();
            }

            if (id.IsGuest)
            {
                // with guests, we don't really care about expiration
                // we want them to reuse their current ID as it makes them easier to ban
                if (id.IsExpired())
                {
                    return Ok(CreateGuestToken(id.UserId));
                }
                return Ok(token);
            }

            // user token, but not authenticated. bounce them back to login
            return Unauthorized();
        }

        public class GuestChatRequest
        {
            public string Data { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GuestChatToken([FromBody] GuestChatRequest req)
        {
            if (siteConfig.EnableCaptcha)
            {
                var res = await captcha.Validate(req.Data);
                if (res == null || !res.success)
                {
                    return BadRequest("invalid captcha");
                }
            }

            return Ok(CreateGuestToken(Guid.NewGuid()));
        }

        private string CreateGuestToken(Guid userId)
        {
            // TODO guest nickname collisions are a mess
            var rnd = new Random();
            var id = new IdentityToken()
            {
                Nick = "Guest" + rnd.Next(1000, 10000),
                UserId = userId,
                IsGuest = true,
                Level = (int)ApplicationUser.UserLevel.Normal,
            };

            // long expiration times - we want guests to reuse tokens
            var token = authorizor.CreateToken(id, DateTime.UtcNow.AddDays(GUEST_TOKEN_EXPIRATION));
            return token;
        }

        [HttpGet]
        public async Task<IActionResult> ChatToken()
        {
            var user = await userManager.GetUserAsync(User);

            return Ok(CreateTokenForUser(user));
        }

        private string CreateTokenForUser(ApplicationUser user)
        {
            var id = new IdentityToken()
            {
                Nick = user.UserName,
                UserId = user.Id,
                IsGuest = false,
                IsValidated = user.EmailConfirmed,
                Level = (int)user.Level,
            };

            var token = authorizor.CreateToken(id, DateTime.UtcNow.AddDays(USER_TOKEN_EXPIRATION));

            return token;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string remoteError = null)
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return Redirect(siteConfig.AppUrl);
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                logger.LogInformation(5, "User logged in with {Name} provider.", info.LoginProvider);
                return Redirect(siteConfig.AppUrl);
            }

            if (result.IsLockedOut)
            {
                //kaboom!
                return Unauthorized();
            }

            // If the user does not have an account, then ask the user to create an account.
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            return Redirect(siteConfig.AppUrl);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private async Task<ApplicationUser> GetUser(string nameOrEmail)
        {
            if (string.IsNullOrEmpty(nameOrEmail))
            {
                return null;
            }

            return nameOrEmail.Contains('@')
                ? await userManager.FindByEmailAsync(nameOrEmail)
                : await userManager.FindByNameAsync(nameOrEmail);
        }
    }
}
