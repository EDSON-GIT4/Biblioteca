using System;
using LibraryAPI.Model;

namespace LibraryAPI.Model
{
    public class AutorModelo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; } 
        public bool IsActive { get; set; } = true; // Soft delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relacionamento N:N com Books
        public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    }
}