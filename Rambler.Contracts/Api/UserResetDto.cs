namespace Rambler.Contracts.Api
{
    using System;

    public  class UserResetDto
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; }
        public string VerifyNewPassword { get; set; }
    }
}
