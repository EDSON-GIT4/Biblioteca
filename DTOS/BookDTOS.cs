using System;

namespace LibraryAPI.DTOs
{
    public record CreateBookDTO
    {
        public string? Title { get; init; }
        public string? ISBN { get; init; }
        public DateTime PublishedDate { get; init; }
        public string? Publisher { get; init; }
        public int TotalCopies { get; init; }
        public List<int> AuthorIds { get; init; } = new(); //Basta inserir o ID do author no POST.
        public List<string>? Roles { get; init; }
    }

    public record UpdateBookDTO
    {
        public string? Title { get; init; }
        public string? ISBN { get; init; }
        public DateTime? PublishedDate { get; init; }
        public string? Publisher { get; init; }
        public int? TotalCopies { get; init; }
        public List<int> AuthorIds { get; init; } = new();
    }

    public record BookResponseDTO
    {
        public int Id { get; init; }
        // string? Title e } = string.Empty é a mesma coisa
        public string Title { get; init; } = string.Empty;
        public string ISBN { get; init; } = string.Empty;
        public DateTime PublishedDate { get; init; }
        public string Publisher { get; init; } = string.Empty;
        public int TotalCopies { get; init; }
        public int AvailableCopies { get; init; }
        public List<AuthorDTO> Authors { get; init; } = new();
    }

    public record BookFilterDTO
    {
        public string? Title { get; init; }
        public string? ISBN { get; init; }
        public string? AuthorName { get; init; }
        public int? YearFrom { get; init; }
        public int? YearTo { get; init; }
        public string? Publisher { get; init; }
        public bool? IsAvailable { get; init; }
    }

    public record AuthorDTO
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Role { get; init; } = "Author";
        
    }
}