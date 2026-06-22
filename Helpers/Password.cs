using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace LibraryAPI.Helpers
{
    /// <summary>
    /// Serviço para hash e verificação de senhas de 
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Gera hash da senha usando PBKDF2
        /// </summary>
        public static string HashPassword(string password)
        {
            // Gerar salt aleatório
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Gerar hash
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Retornar salt + hash (separados por ponto)
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        /// <summary>
        /// Verifica se a senha corresponde ao hash
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = parts[1];

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hash == hashed;
        }
    }
}