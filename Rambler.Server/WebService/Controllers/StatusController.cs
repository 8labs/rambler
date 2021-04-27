namespace Rambler.Server.WebService.Controllers
{
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class StatusController : ControllerBase
    {
        readonly ILogger logger;
        readonly ApplicationDbContext db;
        readonly UserManager<ApplicationUser> userManager;
        readonly StateMutator mutator;
        readonly IResponsePublisher dist;

        public StatusController(
            UserManager<ApplicationUser> userManager,
            StateMutator mutator,
            ApplicationDbContext db,
            IResponsePublisher dist,
            ILogger<ChannelController> logger)
        {
            this.userManager = userManager;
            this.logger = logger;
            this.db = db;
            this.mutator = mutator;
            this.dist = dist;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserCount()
        {
            var count = await mutator.Enqueue((state) => state.GetServerUserCount());

            return Ok(count);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoomUserCount(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Ok(0);
            }

            var count = await mutator.Enqueue((state) => state.GetRoomUserCountByName(name));
            return Ok(count);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveUserCount()
        {
            DateTime ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
            var count = await db.Users.LongCountAsync(u => u.LastSeenDate > ninetyDaysAgo);
            return Ok(count);
        }
    }
}
