namespace Rambler.Server.WebService.Controllers
{
    using System.Collections.Generic;
    using Contracts.Api;
    using Contracts.Responses;
    using Database;
    using Database.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using State;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [Authorize]
    public class ChatController : ControllerBase
    {
        public const int MAX_MESSAGES = 100;

        readonly ILogger logger;
        readonly StateMutator mutator;
        readonly Socket.IAuthorize authorizor;
        readonly ApplicationDbContext db;
        readonly UserManager<ApplicationUser> userManager;

        public ChatController(
            StateMutator mutator,
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            Socket.IAuthorize authorizor,
            ILogger<ChatController> logger)
        {
            this.logger = logger;
            this.authorizor = authorizor;
            this.mutator = mutator;
            this.db = db;
            this.userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetRooms(string search)
        {
            var results = await mutator.Enqueue(state =>
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    return state.GetPublicChannels();
                }
                else
                {
                    return state.GetPublicChannels(name => name.ToLower().StartsWith(search.ToLower()));
                }
            });

            return Ok(results);
        }

        private async Task<Guid?> GetUserId(string token)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await userManager.GetUserAsync(User);
                return user.Id;
            }

            var identity = authorizor.Authorize(token, true);
            if (identity != null)
            {
                return identity.UserId;
            }

            return null;
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SubscriptionMessages([FromBody] GetSubscriptionMessages request)
        {
            var userId = await GetUserId(request.Token);
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            bool canSearch = await mutator.Enqueue(s => s.IsUserInChannel(request.Id, userId.Value));
            if (!canSearch)
            {
                return BadRequest("Not in channel");
            }

            var posts = await db.ChannelPosts
                .Where(m => m.Subscription == request.Id && m.Id > request.Last)
                .OrderByDescending(m => m.Id) // grab the _last_ max
                .Take(MAX_MESSAGES)
                .ToListAsync();

            var results = posts.Select(p => new Response<ChannelMessageResponse>()
            {
                Id = p.Id,
                Timestamp = ((DateTimeOffset)p.CreatedOn).ToUnixTimeMilliseconds(),
                Type = ChannelMessageResponse.KEY,
                Subscription = p.Subscription,
                Data = new ChannelMessageResponse()
                {
                    Message = p.Message,
                    UserId = p.Originator,
                    Nick = p.Nick,
                    Type = p.Type,
                }
            }).ToList();

            // note: client will get these in desc, but can just process in reverse

            return Ok(results);
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DmMessages([FromBody] GetDmMessages request)
        {
            var userId = await GetUserId(request.Token);
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var posts = await db.ChannelPosts
                .Where(m => ((m.Subscription == request.UserId && m.Originator == userId.Value) || (m.Subscription == userId.Value && m.Originator == request.UserId)) && m.Id > request.Last)
                .OrderByDescending(m => m.Id) // grab the _last_ max
                .Take(MAX_MESSAGES)
                .ToListAsync();

            // client doesn't need the echo user in this case since both users are always present
            // (unlike normal echos which are f'ing weird)
            var results = posts.Select(p => new Response<DirectMessageResponse>()
            {
                Id = p.Id,
                Timestamp = ((DateTimeOffset)p.CreatedOn).ToUnixTimeMilliseconds(),
                Type = DirectMessageResponse.KEY,
                Subscription = p.Subscription,
                Data = new DirectMessageResponse()
                {
                    Message = p.Message,
                    UserId = p.Originator,
                    Nick = p.Nick,
                    Type = p.Type,
                }
            }).ToList();

            return Ok(results);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserSettings()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var settings = new UserSettingsDto()
            {
                Ignores = await GetUserIgnores(user)
            };

            return Ok(settings);
        }

        [HttpPost]
        public async Task<IActionResult> AddIgnore([FromBody] IgnoreDto request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (user.Id == request.IgnoreId)
            {
                // don't ignore yourself silly.
                return Ok(await GetUserIgnores(user));
            }

            var exists = await db.UserIgnores
                .AnyAsync(i => i.UserId == user.Id && i.IgnoreId == request.IgnoreId);

            if (!exists)
            {
                var target = await db.UserConnections
                    .Where(c => c.UserId == request.IgnoreId)
                    .FirstOrDefaultAsync();

                var inick = (target == null || target.Nick == null)
                    ? "unknown"
                    : target.Nick;

                var isguest = inick.StartsWith("Guest");

                db.UserIgnores.Add(new UserIgnore()
                {
                    IgnoredOn = DateTime.UtcNow,
                    IgnoreId = request.IgnoreId,
                    IgnoreNick = inick,
                    IsGuestIgnore = isguest,
                    User = user,
                });

                await db.SaveChangesAsync();
            }

            return Ok(await GetUserIgnores(user));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveIgnore([FromBody] IgnoreDto request)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var exists = await db.UserIgnores
                .Where(i => i.UserId == user.Id && i.IgnoreId == request.IgnoreId)
                .ToListAsync();

            if (exists.Count > 0)
            {
                db.UserIgnores.RemoveRange(exists);
                await db.SaveChangesAsync();
            }

            return Ok(await GetUserIgnores(user));
        }

        private async Task<IList<IgnoreDto>> GetUserIgnores(ApplicationUser user)
        {
            var ignores = await db.UserIgnores
               .Where(i => i.UserId == user.Id)
               .ToListAsync();

            return ignores.Select(i => new IgnoreDto()
            {
                Id = i.Id,
                IgnoredOn = i.IgnoredOn,
                IgnoreId = i.IgnoreId,
                IgnoreNick = i.IgnoreNick,
                UserId = i.UserId
            }).ToList();
        }

    }
}
