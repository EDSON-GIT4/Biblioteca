using System;
using LibraryAPI.Model;

namespace LibraryAPI.Model
{
    public class Loan
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public UsuarioModelo User { get; set; } = null!;
    
        public int BookId { get; set; }
        public LivroModelo Book { get; set; } = null!;
    
        public DateTime LoanDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public LoanStatus Status { get; set; } = LoanStatus.Active;
        public decimal? FineAmount { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public enum LoanStatus
    {
        Active,
        Returned,
        Overdue,
        Lost
    }
}