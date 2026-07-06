using System;
using System.Text;

namespace BetterSubnautica.Extensions
{
    public static class StringExtensions
    {
        public static string Base64Encode(this string text)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string data)
        {
            var base64EncodedBytes = Convert.FromBase64String(data);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
