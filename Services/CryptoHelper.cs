using System;
using System.Security.Cryptography;
using System.Text;

namespace TerranovaDemo.Services
{
    public static class CryptoHelper
    {
        /// <summary>
        /// Retorna el hash SHA-256 en mayúsculas hex (igual que Convert.ToHexString)
        /// </summary>
        public static string HashPassword(string password)
        {
            if (password is null) password = string.Empty;
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
