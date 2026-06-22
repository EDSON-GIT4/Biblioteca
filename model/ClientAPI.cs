using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Model
{
    public class ClienteModelo
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string ClientId { get; set; } = string.Empty; // Identificador público
        
        [Required]
        public string ClientSecretHash { get; set; } = string.Empty; // Hash do segredo
        
        [Required]
        [StringLength(200)]
        public string Descricao { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? ContatoEmail { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastAccessedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Rate Limiting
        public int RateLimitPerMinute { get; set; } = 60;
        public int RateLimitPerDay { get; set; } = 10000;
        
        // Permissões específicas
        public bool CanAccessBooks { get; set; } = true;
        public bool CanAccessUsers { get; set; } = false;
        public bool CanAccessLoans { get; set; } = true;
        public bool CanManageAuthors { get; set; } = false;
        
        // Lista de IPs permitidos (separados por vírgula)
        [StringLength(500)]
        public string? AllowedIPs { get; set; }
        
        // Webhooks
        [StringLength(500)]
        public string? WebhookUrl { get; set; }
        
        // Relacionamento com logs de acesso
        public ICollection<ClienteAccessLog> AccessLogs { get; set; } = new List<ClienteAccessLog>();
    }
    
    // Model/ClienteAccessLog.cs
    public class ClienteAccessLog
    {
        [Key]
        public long Id { get; set; }
        
        public int ClienteId { get; set; }
        public ClienteModelo Cliente { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string Endpoint { get; set; } = string.Empty;
        
        [Required]
        [StringLength(10)]
        public string Method { get; set; } = string.Empty;
        
        public int StatusCode { get; set; }
        
        [StringLength(45)]
        public string? IPAddress { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public long ResponseTimeMs { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
    }
}