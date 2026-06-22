using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Model;

namespace LibraryAPI.Services
{
    public class ClienteService
    {
        private readonly LibraryDbContext _context;
        private readonly ILogger<ClienteService> _logger;
        private readonly AuthServiceClient _authClient;

        public ClienteService(
            LibraryDbContext context,
            ILogger<ClienteService> logger,
            AuthServiceClient authClient)
        {
            _context = context;
            _logger = logger;
            _authClient = authClient;
        }

        /// <summary>
        /// Criar novo cliente da API
        /// </summary>
        public async Task<ClienteCredentialsResponseDTO> CreateClienteAsync(CreateClienteDTO dto)
        {
            var clientId = _authClient.GenerateClientId();
            
            // Garantir que o Client ID é único
            while (await _context.Clientes.AnyAsync(c => c.ClientId == clientId))
            {
                clientId = _authClient.GenerateClientId();
            }

            var clientSecret = _authClient.GenerateClientSecret();
            var clientSecretHash = _authClient.HashClientSecret(clientSecret);

            var cliente = new ClienteModelo
            {
                Nome = dto.Nome,
                ClientId = clientId,
                ClientSecretHash = clientSecretHash,
                Descricao = dto.Descricao,
                ContatoEmail = dto.ContatoEmail,
                AllowedIPs = dto.AllowedIPs,
                //WebhookUrl = dto.WebhookUrl,
                RateLimitPerMinute = dto.RateLimitPerMinute,
                RateLimitPerDay = dto.RateLimitPerDay,
                CanAccessBooks = dto.CanAccessBooks,
                CanAccessUsers = dto.CanAccessUsers,
                CanAccessLoans = dto.CanAccessLoans,
                CanManageAuthors = dto.CanManageAuthors,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            // Gerar token inicial
            var permissions = new Dictionary<string, bool>
            {
                { "books", cliente.CanAccessBooks },
                { "users", cliente.CanAccessUsers },
                { "loans", cliente.CanAccessLoans },
                { "authors", cliente.CanManageAuthors }
            };

            var token = _authClient.GenerateJwtToken(
                cliente.Id, 
                cliente.ClientId, 
                cliente.Nome,
                permissions);

            _logger.LogInformation(
                "Novo cliente API criado: {Nome} (ClientId: {ClientId})", 
                cliente.Nome, 
                cliente.ClientId);

            return new ClienteCredentialsResponseDTO
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                ClientId = cliente.ClientId,
                ClientSecret = clientSecret, //  Única vez que será mostrado
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
        }

        /// <summary>
        /// Autenticar cliente usando Client ID e Secret
        /// </summary>
        public async Task<ClienteAuthResponseDTO?> AuthenticateClientAsync(
            string clientId, 
            string clientSecret)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => 
                    c.ClientId == clientId && 
                    c.IsActive);

            if (cliente == null)
            {
                _logger.LogWarning(
                    "Tentativa de autenticação com ClientId inválido: {ClientId}", 
                    clientId);
                return null;
            }

            if (!_authClient.VerifyClientSecret(clientSecret, cliente.ClientSecretHash))
            {
                _logger.LogWarning(
                    "Client Secret inválido para cliente: {ClientId}", 
                    clientId);
                return null;
            }

            // Atualizar último acesso
            cliente.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Gerar novo token
            var permissions = new Dictionary<string, bool>
            {
                { "books", cliente.CanAccessBooks },
                { "users", cliente.CanAccessUsers },
                { "loans", cliente.CanAccessLoans },
                { "authors", cliente.CanManageAuthors }
            };

            var token = _authClient.GenerateJwtToken(
                cliente.Id, 
                cliente.ClientId, 
                cliente.Nome,
                permissions);

            _logger.LogInformation(
                "Cliente autenticado com sucesso: {ClientId}", 
                clientId);

            return new ClienteAuthResponseDTO
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = 28800, // 8 horas em segundos
                ClientId = cliente.ClientId,
                ClientName = cliente.Nome
            };
        }

        /// <summary>
        /// Obter informações do cliente
        /// </summary>
        public async Task<ClienteResponseDTO?> GetClienteByIdAsync(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.AccessLogs)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
                return null;

            var logs = cliente.AccessLogs;
            
            return new ClienteResponseDTO
            {
                Id = cliente.Id,
                Nome = cliente.Nome,
                ClientId = cliente.ClientId,
                Descricao = cliente.Descricao,
                ContatoEmail = cliente.ContatoEmail,
                CreatedAt = cliente.CreatedAt,
                LastAccessedAt = cliente.LastAccessedAt,
                IsActive = cliente.IsActive,
                RateLimitPerMinute = cliente.RateLimitPerMinute,
                RateLimitPerDay = cliente.RateLimitPerDay,
                CanAccessBooks = cliente.CanAccessBooks,
                CanAccessUsers = cliente.CanAccessUsers,
                CanAccessLoans = cliente.CanAccessLoans,
                CanManageAuthors = cliente.CanManageAuthors,
                TotalRequests = logs.Count,
                AverageResponseTimeMs = logs.Any() 
                    ? logs.Average(l => l.ResponseTimeMs) 
                    : 0
            };
        }

        /// <summary>
        /// Listar todos os clientes
        /// </summary>
        public async Task<List<ClienteResponseDTO>> GetAllClientesAsync()
        {
            return await _context.Clientes
                .Include(c => c.AccessLogs)
                .Select(c => new ClienteResponseDTO
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    ClientId = c.ClientId,
                    Descricao = c.Descricao,
                    ContatoEmail = c.ContatoEmail,
                    CreatedAt = c.CreatedAt,
                    LastAccessedAt = c.LastAccessedAt,
                    IsActive = c.IsActive,
                    RateLimitPerMinute = c.RateLimitPerMinute,
                    RateLimitPerDay = c.RateLimitPerDay,
                    CanAccessBooks = c.CanAccessBooks,
                    CanAccessUsers = c.CanAccessUsers,
                    CanAccessLoans = c.CanAccessLoans,
                    CanManageAuthors = c.CanManageAuthors,
                    TotalRequests = c.AccessLogs.Count,
                    AverageResponseTimeMs = c.AccessLogs.Any() 
                        ? c.AccessLogs.Average(l => l.ResponseTimeMs) 
                        : 0
                })
                .ToListAsync();
        }

        /// <summary>
        /// Revogar acesso do cliente
        /// </summary>
        public async Task<bool> DeactivateClienteAsync(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return false;

            cliente.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Cliente desativado: {Nome} (ClientId: {ClientId})", 
                cliente.Nome, 
                cliente.ClientId);

            return true;
        }

        /// <summary>
        /// Registrar log de acesso
        /// </summary>
        public async Task LogAccessAsync(
            int clienteId, 
            string endpoint, 
            string method, 
            int statusCode,
            string? ipAddress,
            long responseTimeMs,
            string? userAgent)
        {
            var log = new ClienteAccessLog
            {
                ClienteId = clienteId,
                Endpoint = endpoint,
                Method = method,
                StatusCode = statusCode,
                IPAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
                ResponseTimeMs = responseTimeMs,
                UserAgent = userAgent
            };

            _context.ClienteAccessLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}