using System;
using System.Security.Cryptography;
using System.Text;

namespace HR.Web.Helpers
{
    /// <summary>
    /// RFC 6238 TOTP for MFA on .NET 4.0 (replaces Google.Authenticator package).
    /// </summary>
    public static class TotpHelper
    {
        private const int DefaultStepSeconds = 30;
        private const int DefaultDigits = 6;

        public static string GenerateSetupUri(string issuer, string accountName, string secret)
        {
            return string.Format(
                "otpauth://totp/{0}:{1}?secret={2}&issuer={0}",
                Uri.EscapeDataString(issuer ?? string.Empty),
                Uri.EscapeDataString(accountName ?? string.Empty),
                secret ?? string.Empty);
        }

        public static bool ValidatePin(string secret, string pin, int allowedDriftSteps)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(pin))
            {
                return false;
            }

            pin = pin.Trim();
            if (pin.Length != DefaultDigits)
            {
                return false;
            }

            var counter = GetCurrentCounter(DateTime.UtcNow);
            for (var drift = -allowedDriftSteps; drift <= allowedDriftSteps; drift++)
            {
                var expected = ComputeTotp(secret, counter + drift);
                if (SlowEquals(expected, pin))
                {
                    return true;
                }
            }

            return false;
        }

        private static long GetCurrentCounter(DateTime utcNow)
        {
            var unix = (long)(utcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            return unix / DefaultStepSeconds;
        }

        private static string ComputeTotp(string secret, long counter)
        {
            var key = DecodeBase32(secret);
            var counterBytes = new byte[8];
            for (var i = 7; i >= 0; i--)
            {
                counterBytes[i] = (byte)(counter & 0xff);
                counter = counter >> 8;
            }

            using (var hmac = new HMACSHA1(key))
            {
                var hash = hmac.ComputeHash(counterBytes);
                var offset = hash[hash.Length - 1] & 0x0f;
                var binary =
                    ((hash[offset] & 0x7f) << 24) |
                    ((hash[offset + 1] & 0xff) << 16) |
                    ((hash[offset + 2] & 0xff) << 8) |
                    (hash[offset + 3] & 0xff);

                var otp = binary % (int)Math.Pow(10, DefaultDigits);
                return otp.ToString(new string('0', DefaultDigits));
            }
        }

        private static byte[] DecodeBase32(string secret)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            secret = (secret ?? string.Empty).Trim().Replace(" ", string.Empty).ToUpperInvariant();
            var output = new System.Collections.Generic.List<byte>();
            var bits = 0;
            var value = 0;

            foreach (var c in secret)
            {
                var index = alphabet.IndexOf(c);
                if (index < 0)
                {
                    continue;
                }

                value = (value << 5) | index;
                bits += 5;
                if (bits >= 8)
                {
                    output.Add((byte)((value >> (bits - 8)) & 0xff));
                    bits -= 8;
                }
            }

            return output.ToArray();
        }

        private static bool SlowEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }

            return diff == 0;
        }
    }
}
