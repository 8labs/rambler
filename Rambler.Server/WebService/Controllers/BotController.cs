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
    using System.Linq;
    using System.Threading.Tasks;

    [Authorize]
    public class BotController : ControllerBase
    {
        readonly ILogger logger;
        readonly StateMutator mutator;
        readonly ApplicationDbContext db;

        public BotController(
            StateMutator mutator,
            ApplicationDbContext db,
            ILogger<BotController> logger)
        {
            this.logger = logger;
            this.mutator = mutator;
            this.db = db;
        }

        // get api token (requires user authorization)
        [HttpGet]
        public async Task<IActionResult> GetToken(Guid botId)
        {
            return Ok();
        }

        // send notification to channel

        // ... whatever other actions we like
    }
}
