using System;
using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Model;


namespace LibraryAPI.Services
{
    public class LoanService
    {
        private readonly LibraryDbContext _context;
        private readonly ILogger<LoanService> _logger;

        public LoanService(LibraryDbContext context, ILogger<LoanService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LoanResponseDTO> CreateLoanAsync(CreateLoanDTO dto)
        {
            // Validar usuário
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                throw new InvalidOperationException("Usuário não encontrado.");

            // Validar livro
            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null)
                throw new InvalidOperationException("Livro não encontrado.");

            if (book.AvailableCopies <= 0)
                throw new InvalidOperationException("Não há cópias disponíveis deste livro.");

            // Verificar limite de empréstimos do usuário
            var activeLoans = await _context.Loans
                .CountAsync(l => l.UserId == dto.UserId && l.Status == LoanStatus.Active);

            if (activeLoans >= user.MaxLoansAllowed)
                throw new InvalidOperationException(
                    $"Usuário já possui {user.MaxLoansAllowed} empréstimos ativos.");

            // Verificar se usuário já tem este livro emprestado
            var alreadyLoaned = await _context.Loans
                .AnyAsync(l => l.UserId == dto.UserId && 
                            l.BookId == dto.BookId && 
                            l.Status == LoanStatus.Active);

            if (alreadyLoaned)
                throw new InvalidOperationException("Usuário já possui este livro emprestado.");

            // Validar data de devolução
            if (dto.LoanDays <= 0 || dto.LoanDays > 30)
                throw new InvalidOperationException("O período de empréstimo deve ser entre 1 e 30 dias.");

            var loan = new Loan
            {
                UserId = dto.UserId,
                BookId = dto.BookId,
                LoanDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(dto.LoanDays)
            };

            // Atualizar disponibilidade
            book.AvailableCopies--;

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Empréstimo criado: Livro {BookId} para Usuário {UserId}", 
                dto.BookId, dto.UserId);

            return MapToResponseDTO(loan);
        }

        public async Task<LoanResponseDTO> ReturnBookAsync(int loanId)
        {
            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == loanId);

            if (loan == null)
                throw new InvalidOperationException("Empréstimo não encontrado.");

            if (loan.Status != LoanStatus.Active)
                throw new InvalidOperationException("Este empréstimo já foi finalizado.");

            loan.ReturnDate = DateTime.UtcNow;
            loan.Status = LoanStatus.Returned;

            // Calcular multa se estiver atrasado
            if (DateTime.UtcNow > loan.DueDate)
            {
                var daysLate = (DateTime.UtcNow - loan.DueDate).Days;
                loan.FineAmount = daysLate * 1.50m; // R$ 1,50 por dia de atraso
                loan.Status = LoanStatus.Overdue;
            }

            // Atualizar disponibilidade
            loan.Book.AvailableCopies++;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Livro devolvido: Empréstimo {LoanId}", loanId);
            return MapToResponseDTO(loan);
        }

        public async Task<IEnumerable<LoanResponseDTO>> GetActiveLoansAsync()
        {
            var loans = await _context.Loans
                .Include(l => l.User)
                .Include(l => l.Book)
                .Where(l => l.Status == LoanStatus.Active)
                .OrderBy(l => l.DueDate)
                .ToListAsync();

            return loans.Select(MapToResponseDTO);
        }

        private static LoanResponseDTO MapToResponseDTO(Loan loan)
        {
            return new LoanResponseDTO
            {
                Id = loan.Id,
                UserId = loan.UserId,
                UserName = loan.User?.Name ?? "N/A",
                BookId = loan.BookId,
                BookTitle = loan.Book?.Title ?? "N/A",
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                ReturnDate = loan.ReturnDate,
                Status = loan.Status.ToString(),
                FineAmount = loan.FineAmount
            };
        }
    }
}