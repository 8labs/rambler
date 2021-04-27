namespace Rambler.Contracts.Api
{
    using System.Collections.Generic;

    public class UserSettingsDto
    {
        public IList<IgnoreDto> Ignores { get; set; }
    }
}
