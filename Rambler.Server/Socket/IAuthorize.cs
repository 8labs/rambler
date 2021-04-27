namespace Rambler.Server.Socket
{
    using Contracts.Api;
    using System;

    public interface IAuthorize
    {
        IdentityToken Authorize(string token, bool validateExpiration);
        bool TryParse(string token, out IdentityToken identity);
        string CreateToken(IdentityToken token, DateTimeOffset expires);
    }
}
