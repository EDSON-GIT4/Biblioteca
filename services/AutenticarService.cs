using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;  

namespace LibraryAPI.Services
{
    /// <summary>
    /// Serviço responsável pela geração e validação de tokens JWT
    /// </summary>
    public class AuthServiceUser
    {
        private readonly IConfiguration _configuration;

        public AuthServiceUser(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gera um token JWT para o usuário autenticado
        /// </summary>
        public string GenerateJwtToken(int userId, string email, string name)
        {
            var secret = _configuration["Jwt:Secret"] 
                ?? throw new InvalidOperationException("JWT Secret não configurado");
            
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)); //Siga o padão só use UTF8, se ja é padrão nos outros arquivos, pois da erro de token diferente
            
            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("token_type", "user"), //identifica o tipo
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, 
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            var expirationHours = double.Parse(
                _configuration["Jwt:ExpirationHours"] ?? "24");

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Valida um token JWT e retorna os claims principais
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secret = _configuration["Jwt:Secret"] 
                    ?? throw new InvalidOperationException("JWT Secret não configurado");
                
                var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)); //Use UTF8 se for padrão no projeto

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(
                    token, validationParameters, out _);
                
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extrai o ID do usuário do token
        /// </summary>
        public int? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            return int.TryParse(userIdClaim, out int userId) ? userId : null;
        }
    }

    public class AuthServiceClient
    {
        private readonly IConfiguration _configuration;

        public AuthServiceClient(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gera um Client ID único
        /// </summary>
        public string GenerateClientId()
        {
            return $"client_{Guid.NewGuid():N}".ToLower();
        }

        /// <summary>
        /// Gera um Client Secret seguro
        /// </summary>
        public string GenerateClientSecret()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                [..64]; // 64 caracteres
        }

        /// <summary>
        /// Hash do Client Secret para armazenamento
        /// </summary>
        public string HashClientSecret(string clientSecret)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(clientSecret));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Verifica se o Client Secret está correto
        /// </summary>
        public bool VerifyClientSecret(string clientSecret, string hash)
        {
            var hashToCheck = HashClientSecret(clientSecret);
            return hashToCheck == hash;
        }

        /// <summary>
        /// Gera token JWT para cliente da API
        /// </summary>
        public string GenerateJwtToken(
            int clienteId, 
            string clientId, 
            string nome,
            Dictionary<string, bool>? permissions = null)
        {
            var secret = _configuration["Jwt:Secret"] 
                ?? throw new InvalidOperationException("JWT Secret não configurado");
            
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)); //Use UTF8 se for padrão no projeto, pois da erro de token diferente
            
            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("token_type", "client"), // Identifica que é um cliente
                new Claim("client_type", "api_consumer"),
                new Claim(ClaimTypes.NameIdentifier, clienteId.ToString()),
                new Claim("client_id", clientId),
                new Claim(ClaimTypes.Name, nome),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, 
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, clientId)
            };

            // Adicionar permissões como claims
            if (permissions != null)
            {
                foreach (var permission in permissions)
                {
                    claims.Add(new Claim($"permission_{permission.Key.ToLower()}", 
                        permission.Value.ToString().ToLower()));
                }
            }

            var expirationHours = double.Parse(
                _configuration["Jwt:ClientExpirationHours"] ?? "8"); // Clientes expiram mais rápido

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:ClientAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Gera token de refresh para cliente
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Valida um token JWT de cliente
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var secret = _configuration["Jwt:Secret"] 
                    ?? throw new InvalidOperationException("JWT Secret não configurado");
                
                var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)); //

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:ClientAudience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1) // Pequena margem para clientes
                };

                var principal = tokenHandler.ValidateToken(
                    token, validationParameters, out _);
                
                // Verificar se é realmente um token de cliente
                var tokenType = principal.FindFirst("token_type")?.Value;
                if (tokenType != "client")
                    return null;
                
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extrai o Client ID do token
        /// </summary>
        public string? GetClientIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst("client_id")?.Value;
        }
    }
}
