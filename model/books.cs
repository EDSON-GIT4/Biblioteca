using System;
using LibraryAPI.Model;

namespace LibraryAPI.Model
{
    public class LivroModelo
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public string Publisher { get; set; } = string.Empty;
        public int AvailableCopies { get; set; }
        public bool IsActive { get; set; } = true; // Soft delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relacionamento N:N com Authors
        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    
        // Relacionamento 1:N com Loans
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}