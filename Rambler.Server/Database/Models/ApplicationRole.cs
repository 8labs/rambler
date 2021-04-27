namespace Rambler.Server.Database.Models
{
    using System;
    using Microsoft.AspNetCore.Identity;

    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationRole : IdentityRole<Guid>
    {
    }
}
