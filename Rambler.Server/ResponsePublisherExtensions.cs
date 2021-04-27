namespace Rambler.Server
{
    using Contracts.Responses;

    using System;
    using System.Threading.Tasks;

    public static class ResponsePublisherExtensions
    {
        public static async Task<Response<ErrorResponse>> PublishError(this IResponsePublisher pub, Guid subscriber, ErrorResponse.ErrorCode code)
        {
            var resp = new Response<ErrorResponse>()
            {
                Subscription = subscriber,
                Data = new ErrorResponse() { Code = (int)code }
            };

            await pub.Publish(resp);
            return resp;
        }
    }
}
