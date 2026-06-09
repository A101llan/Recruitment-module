using System;
using System.Security.Cryptography;

namespace HR.Web.Helpers
{
    /// <summary>
    /// PBKDF2-HMAC-SHA1 for .NET 4.0 where Rfc2898DeriveBytes lacks an explicit iteration constructor.
    /// </summary>
    internal static class Pbkdf2Helper
    {
        public static byte[] DeriveKey(string password, byte[] salt, int iterations, int keyLength)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            if (salt == null)
            {
                throw new ArgumentNullException("salt");
            }

            if (iterations < 1)
            {
                throw new ArgumentOutOfRangeException("iterations");
            }

            if (keyLength < 1)
            {
                throw new ArgumentOutOfRangeException("keyLength");
            }

            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            using (var hmac = new HMACSHA1(passwordBytes))
            {
                var hashLength = hmac.HashSize / 8;
                var blocksNeeded = (int)Math.Ceiling(keyLength / (double)hashLength);
                var result = new byte[keyLength];
                var offset = 0;

                for (var block = 1; block <= blocksNeeded; block++)
                {
                    var blockBytes = Pbkdf2Block(hmac, salt, iterations, block);
                    var copyLength = Math.Min(hashLength, keyLength - offset);
                    Buffer.BlockCopy(blockBytes, 0, result, offset, copyLength);
                    offset += copyLength;
                }

                return result;
            }
        }

        private static byte[] Pbkdf2Block(HMACSHA1 hmac, byte[] salt, int iterations, int blockIndex)
        {
            var blockIndexBytes = new byte[4];
            blockIndexBytes[0] = (byte)((blockIndex >> 24) & 0xff);
            blockIndexBytes[1] = (byte)((blockIndex >> 16) & 0xff);
            blockIndexBytes[2] = (byte)((blockIndex >> 8) & 0xff);
            blockIndexBytes[3] = (byte)(blockIndex & 0xff);

            var input = new byte[salt.Length + 4];
            Buffer.BlockCopy(salt, 0, input, 0, salt.Length);
            Buffer.BlockCopy(blockIndexBytes, 0, input, salt.Length, 4);

            var u = hmac.ComputeHash(input);
            var result = (byte[])u.Clone();

            for (var i = 1; i < iterations; i++)
            {
                u = hmac.ComputeHash(u);
                for (var j = 0; j < result.Length; j++)
                {
                    result[j] ^= u[j];
                }
            }

            return result;
        }
    }
}
