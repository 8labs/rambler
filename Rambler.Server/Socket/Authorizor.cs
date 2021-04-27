namespace Rambler.Server.Socket
{
    using Contracts.Api;
    using JWT;
    using JWT.Algorithms;
    using JWT.Serializers;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Options;
    using System;
    using System.Collections.Generic;
    using Utility;

    /// <summary>
    /// quick authorizer for jwt tokens coming from the client
    /// </summary>
    public class Authorizor : IAuthorize
    {
        readonly IJsonSerializer serializer = new JsonNetSerializer();
        readonly IDateTimeProvider provider = new UtcDateTimeProvider();
        readonly IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
        readonly IJwtValidator validator;
        readonly IJwtDecoder decoder;
        readonly IJwtEncoder encoder;
        readonly TokenOptions config;
        readonly IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
        readonly ILogger<Authorizor> log;

        public Authorizor(IOptions<TokenOptions> config, ILogger<Authorizor> log)
        {
            validator = new JwtValidator(serializer, provider);
            decoder = new JwtDecoder(serializer, validator, urlEncoder);
            encoder = new JwtEncoder(algorithm, serializer, urlEncoder);
            this.config = config.Value;
            this.log = log;
        }

        public bool TryParse(string token, out IdentityToken identity)
        {
            try
            {
                identity = decoder.DecodeToObject<IdentityToken>(token, config.Secret, verify: false);
                return true;
            }
            catch (Exception)
            {
                identity = null;
                return false;
            }
        }

        public IdentityToken Authorize(string token, bool validateExpiration)
        {
            try
            {
                var json = decoder.Decode(token, config.Secret, verify: true);

                // TODO: this probably needs a catch to double check that the data is valid
                // in case there's bugs in the server (or stale data), I'd rather have it regen.
                var id = JsonConvert.DeserializeObject<IdentityToken>(json);

                // why do I even use third party libs when basic shit like this doesn't work...
                if (validateExpiration && id.IsExpired())
                {
                    return null;
                }

                return id;
            }
            catch (TokenExpiredException)
            {
                log.LogDebug("Failed to authorize: TokenExpiredException");
                return null;
            }
            catch (SignatureVerificationException)
            {
                log.LogDebug("Failed to authorize: TokenExpiredException");
                return null;
            }
            catch (Exception ex)
            {
                log.LogDebug("Failed to authorize: {Message}", ex.Message);
                // we don't really care why it fails for now.
                return null;
            }
        }

        public string CreateToken(IdentityToken token, DateTimeOffset expires)
        {
            token.Expires = expires.ToUnixTimeSeconds();

            // this doesn't seem to work :|
            //var headers = new Dictionary<string, object>
            //{
            //    { "exp", token.Expires },
            //};            

            return encoder.Encode(token, config.Secret);
        }


    }
}
