using System.Security.Cryptography;
using System.Text;

namespace JJDevHub.Api.Invites
{
    public static class InviteCode
    {
        public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        public const int Length = 10;

        public static string Create()
        {
            var bytes = new byte[Length];
            RandomNumberGenerator.Fill(bytes);
            var chars = new char[Length];
            for (int index = 0; index < Length; index++)
            {
                chars[index] = Alphabet[bytes[index] % Alphabet.Length];
            }

            return new string(chars);
        }

        public static string Hash(string code)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(code)));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string Normalize(string code)
        {
            var builder = new StringBuilder(code.Length);
            foreach (var character in code)
            {
                if (character is '-' or ' ')
                {
                    continue;
                }

                builder.Append(char.ToUpperInvariant(character));
            }

            return builder.ToString();
        }
    }
}
