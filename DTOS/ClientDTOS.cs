using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs
{
    // ============ ENTRADA ============
    
    /// <summary>
    /// DTO para criar novo cliente da API
    /// </summary>
    public record CreateClienteDTO
    {
        [Required(ErrorMessage = "Nome do cliente é obrigatório")]
        [StringLength(100, MinimumLength = 3)]
        public string Nome { get; init; } = string.Empty;
        
        [Required(ErrorMessage = "Descrição é obrigatória")]
        [StringLength(200)]
        public string Descricao { get; init; } = string.Empty;
        
        [EmailAddress]
        [StringLength(100)]
        public string? ContatoEmail { get; init; }
        
        [StringLength(500)]
        public string? AllowedIPs { get; init; }
        
        //O acompo WebhookUrl recebe null por enquanto
        //[StringLength(500)]
        //[Url(ErrorMessage = "URL de webhook inválida")]
        //public string? WebhookUrl { get; init; } = null;
        
        [Range(1, 100)]
        public int RateLimitPerMinute { get; init; } = 60;
        
        [Range(1, 100000)]
        public int RateLimitPerDay { get; init; } = 10000;
        
        public bool CanAccessBooks { get; init; } = true;
        public bool CanAccessUsers { get; init; } = false;
        public bool CanAccessLoans { get; init; } = true;
        public bool CanManageAuthors { get; init; } = false;
    }
    
    /// <summary>
    /// DTO para renovar credenciais do cliente
    /// </summary>
    public record RefreshClienteCredentialsDTO
    {
        [Required]
        public string ClientId { get; init; } = string.Empty;
        
        [Required]
        public string ClientSecret { get; init; } = string.Empty;
    }
    
    // ============ SAÍDA ============
    
    /// <summary>
    /// DTO com as credenciais do cliente (retornado apenas na criação)
    /// </summary>
    public record ClienteCredentialsResponseDTO
    {
        public int Id { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string ClientId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty; // ⚠️ Mostrado apenas uma vez!
        public string Token { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
        public string Message { get; init; } = "Guarde o Client Secret em local seguro. Ele não será mostrado novamente.";
    }
    
    /// <summary>
    /// DTO para resposta de autenticação do cliente
    /// </summary>
    public record ClienteAuthResponseDTO
    {
        public string Token { get; init; } = string.Empty;
        public string TokenType { get; init; } = "Bearer";
        public int ExpiresIn { get; init; } // segundos
        public string ClientId { get; init; } = string.Empty;
        public string ClientName { get; init; } = string.Empty;
    }
    
    /// <summary>
    /// DTO para informações públicas do cliente
    /// </summary>
    public record ClienteResponseDTO
    {
        public int Id { get; init; }
        public string Nome { get; init; } = string.Empty;
        public string ClientId { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public string? ContatoEmail { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastAccessedAt { get; init; }
        public bool IsActive { get; init; }
        public int RateLimitPerMinute { get; init; }
        public int RateLimitPerDay { get; init; }
        public bool CanAccessBooks { get; init; }
        public bool CanAccessUsers { get; init; }
        public bool CanAccessLoans { get; init; }
        public bool CanManageAuthors { get; init; }
        public int TotalRequests { get; init; }
        public double AverageResponseTimeMs { get; init; }
    }
}