using System;
using System.Security.Cryptography;
using System.Text;

namespace AspPix
{
    public static class StaticFunction
    {
        public static string Base64Encode(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }

        public static string Base64Decode(string text)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }

        public static int GetTagHash(string tag)
        {
            byte[] buff = Encoding.UTF8.GetBytes(tag);

            byte[] hash = SHA256.HashData(buff);

            return BitConverter.ToInt32(hash, 0);

        }
    }
}