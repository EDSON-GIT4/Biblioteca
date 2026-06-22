using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Model;
using LibraryAPI.Controllers;

namespace LibraryAPI.Services
{
    public class BookService
    {
        private readonly LibraryDbContext _context;
        private readonly ILogger<BookService> _logger;

        public BookService(LibraryDbContext context, ILogger<BookService> logger)
        {
            _context = context;
            _logger = logger;
        }

        //Buscar livro por ID
        public async Task<BookResponseDTO?> GetBookByIdAsync(int id)
        {
            var book = await _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(b => b.Id == id);

            return book != null ? MapToResponseDTO(book) : null;
        }
        public async Task<IEnumerable<BookResponseDTO>> GetBooksAsync(BookFilterDTO filter)
        {
            var query = _context.Books
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrWhiteSpace(filter.Title))
                query = query.Where(b => b.Title.Contains(filter.Title));

            if (!string.IsNullOrWhiteSpace(filter.ISBN))
                query = query.Where(b => b.ISBN == filter.ISBN);

            if (!string.IsNullOrWhiteSpace(filter.AuthorName))
                query = query.Where(b => b.BookAuthors
                    .Any(ba => ba.Author.Name.Contains(filter.AuthorName)));

            if (filter.YearFrom.HasValue)
                query = query.Where(b => b.PublishedDate.Year >= filter.YearFrom.Value);

            if (filter.YearTo.HasValue)
                query = query.Where(b => b.PublishedDate.Year <= filter.YearTo.Value);

            if (!string.IsNullOrWhiteSpace(filter.Publisher))
                query = query.Where(b => b.Publisher.Contains(filter.Publisher));

            if (filter.IsAvailable.HasValue && filter.IsAvailable.Value)
                query = query.Where(b => b.AvailableCopies > 0);

            var books = await query.ToListAsync();

            return books.Select(b => MapToResponseDTO(b));
        }

        public async Task<BookResponseDTO> CreateBookAsync(CreateBookDTO dto)
        {
            // Validar se ISBN já existe
            if (await _context.Books.AnyAsync(b => b.ISBN == dto.ISBN))
                throw new InvalidOperationException($"ISBN {dto.ISBN} já está cadastrado.");

            // Validar autores
            var authorIds = dto.AuthorIds.Distinct().ToList();
            var existingAuthors = await _context.Authors
                .Where(a => authorIds.Contains(a.Id))
                .ToListAsync();

            if (existingAuthors.Count != authorIds.Count)
                throw new InvalidOperationException("Um ou mais autores não encontrados.");

            var book = new LivroModelo
            {
                Title = dto.Title,
                ISBN = dto.ISBN,
                PublishedDate = dto.PublishedDate,
                Publisher = dto.Publisher,
                //TotalCopies = dto.TotalCopies,
                AvailableCopies = dto.TotalCopies
            };

            // Adicionar relacionamentos com autores
            for (int i = 0; i < authorIds.Count; i++)
            {
                book.BookAuthors.Add(new BookAuthor
                {
                    AuthorId = authorIds[i],
                    Role = dto.Roles != null && i < dto.Roles.Count 
                        ? dto.Roles[i] 
                        : "Author"
                });
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Livro criado: {Title} (ID: {Id})", book.Title, book.Id);
            return MapToResponseDTO(book);
        }

        public async Task<bool> SoftDeleteBookAsync(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return false;

            // Verificar se há empréstimos ativos
            var hasActiveLoans = await _context.Loans
                .AnyAsync(l => l.BookId == id && l.Status == LoanStatus.Active);

            if (hasActiveLoans)
                throw new InvalidOperationException(
                    "Não é possível desativar o livro pois existem empréstimos ativos.");

            book.IsActive = false;
            book.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Livro desativado: {Id}", id);
            return true;
        }

        private static BookResponseDTO MapToResponseDTO(LivroModelo book)
        {
            return new BookResponseDTO
            {
                Id = book.Id,
                Title = book.Title,
                ISBN = book.ISBN,
                PublishedDate = book.PublishedDate,
                Publisher = book.Publisher,
                //TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                Authors = book.BookAuthors.Select(ba => new AuthorDTO
                {
                    Id = ba.Author.Id,
                    Name = ba.Author.Name,
                    Role = ba.Role
                }).ToList()
            };
        }
    }
}
