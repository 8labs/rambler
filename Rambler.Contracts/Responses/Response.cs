namespace Rambler.Contracts.Responses
{
    using System;

    public class Response<T> : IResponse
    {
        public long Id { get; set; }

        public string Type { get; set; }

        public long Timestamp { get; set; }

        public Guid Subscription { get; set; }

        public T Data { get; set; }
    }
}
