using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs
{
     // ============ ENTRADA ============
    
    /// <summary>
    /// DTO para criar um novo usuário
    /// </summary>
    public record CreateUserDTO
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
        [Display(Name = "Nome")]
        public string Name { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(200)]
        [Display(Name = "Email")]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; init; } = string.Empty;

        [Compare("Password", ErrorMessage = "Senhas não conferem")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Senha")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }

    /// <summary>
    /// DTO para atualizar usuário
    /// </summary>
    public record UpdateUserDTO
    {
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Nome")]
        public string? Name { get; init; }

        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(200)]
        [Display(Name = "Email")]
        public string? Email { get; init; }

        [Range(1, 10, ErrorMessage = "Máximo de empréstimos deve ser entre 1 e 10")]
        [Display(Name = "Máximo de Empréstimos")]
        public int? MaxLoansAllowed { get; init; }

        [Display(Name = "Ativo")]
        public bool? IsActive { get; init; }
    }

    /// <summary>
    /// DTO para alterar senha
    /// </summary>
    public record ChangePasswordDTO
    {
        [Required(ErrorMessage = "Senha atual é obrigatória")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; init; } = string.Empty;

        [Required(ErrorMessage = "Nova senha é obrigatória")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; init; } = string.Empty;

        [Compare("NewPassword", ErrorMessage = "Senhas não conferem")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; init; } = string.Empty;
    }

    /// <summary>
    /// DTO para login
    /// </summary>
    public record LoginDTO
    {
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        [DataType(DataType.Password)]
        public string Password { get; init; } = string.Empty;
    }

    // ============ SAÍDA ============

    /// <summary>
    /// DTO para resposta de usuário (SEM dados sensíveis)
    /// </summary>
    public record UserResponseDTO
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public DateTime RegistrationDate { get; init; }
        public bool IsActive { get; init; }
        public int MaxLoansAllowed { get; init; }
        public int CurrentLoansCount { get; init; }
        public int AvailableLoans { get; init; }
        // ❌ PasswordHash NUNCA é exposto!
    }

    /// <summary>
    /// DTO para resposta de login bem-sucedido
    /// </summary>
    public record LoginResponseDTO
    {
        public int UserId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Token { get; init; } = string.Empty; // Se usar JWT
        public DateTime ExpiresAt { get; init; }
    }

    // ============ FILTROS ============

    /// <summary>
    /// DTO para filtrar usuários
    /// </summary>
    public record UserFilterDTO
    {
        [Display(Name = "Nome")]
        public string? Name { get; init; }

        [Display(Name = "Email")]
        public string? Email { get; init; }

        [Display(Name = "Ativo")]
        public bool? IsActive { get; init; }

        [Display(Name = "Tem empréstimos ativos")]
        public bool? HasActiveLoans { get; init; }

        [Display(Name = "Cadastrado após")]
        [DataType(DataType.Date)]
        public DateTime? RegisteredAfter { get; init; }

        [Display(Name = "Cadastrado antes")]
        [DataType(DataType.Date)]
        public DateTime? RegisteredBefore { get; init; }

        [Display(Name = "Ordenar por")]
        [RegularExpression("^(name|email|registrationdate|currentloans)$")]
        public string? OrderBy { get; init; } = "name";

        [Display(Name = "Direção")]
        [RegularExpression("^(asc|desc)$")]
        public string? OrderDirection { get; init; } = "asc";

        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 10;
    }
}