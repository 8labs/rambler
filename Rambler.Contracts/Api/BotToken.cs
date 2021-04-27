namespace Rambler.Contracts.Api
{
    using System;
    using System.Collections.Generic;

    public class BotToken
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime Expres { get; set; }

        public IEnumerable<string> Permissions { get; set; }
    }
}
