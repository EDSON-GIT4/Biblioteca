using System;

namespace LibraryAPI.DTOs
{
    public record CreateLoanDTO
    {
        public int UserId { get; init; }
        public int BookId { get; init; }
        public int LoanDays { get; init; } = 15;
    }

    public record LoanResponseDTO
    {
        public int Id { get; init; }
        public int UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public int BookId { get; init; }
        public string BookTitle { get; init; } = string.Empty;
        public DateTime LoanDate { get; init; }
        public DateTime DueDate { get; init; }
        public DateTime? ReturnDate { get; init; }
        public string Status { get; init; } = string.Empty;
        public decimal? FineAmount { get; init; }
    }
}