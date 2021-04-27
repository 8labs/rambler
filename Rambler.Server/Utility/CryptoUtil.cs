namespace Rambler.Server.Utility
{
    using System;
    using System.Security.Cryptography;

    public static class CryptoUtil
    {
        /// <summary>
        /// creates a properly strong random string to generate verification codes.
        /// </summary>
        /// <returns></returns>
        public static string GenerateRandomString(int len)
        {
            // base 64 char = 6bits.
            var byteLen = (len * 6) / 8;

            byte[] random = new Byte[byteLen];
            var rnd = RandomNumberGenerator.Create();
            rnd.GetBytes(random);

            return Convert
                .ToBase64String(random)
                .Substring(0, len);  // trim it if it's over
        }
    }
}
