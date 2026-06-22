using System;
using LibraryAPI.Model;

namespace LibraryAPI.Model
{
    public class BookAuthor
    {
        public int BookId { get; set; }
        public LivroModelo Book { get; set; } = null!;
    
        public int AuthorId { get; set; }
        public AutorModelo Author { get; set; } = null!;
    
        public string Role { get; set; } = "Author"; // Author, Co-author, Editor, etc.
    }
}