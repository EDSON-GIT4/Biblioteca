using System;
using LibraryAPI.Model;

namespace LibraryAPI.Model
{
    public class UsuarioModelo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true; // Soft delete
        public int MaxLoansAllowed { get; set; } = 3;

        // Relacionamento 1:N com Loans
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}