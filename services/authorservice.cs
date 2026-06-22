using Microsoft.EntityFrameworkCore;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Model;
using LibraryAPI.Controllers;

namespace LibraryAPI.Services
{
    public class AuthorService
    {
        private readonly LibraryDbContext _context;
        private readonly ILogger<AuthorService> _logger;

        public AuthorService(LibraryDbContext context, ILogger<AuthorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Buscar autor por ID
        /// </summary>
        public async Task<ResponseAuthorDTO?> GetAuthorByIdAsync(int id)
        {
            var author = await _context.Authors
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (author == null)
                return null;

            return new ResponseAuthorDTO
            {
                Id = author.Id,
                Name = author.Name,
                BirthDate = author.BirthDate,
                IsActive = author.IsActive
            };
        }

        /// <summary>
        /// Listar autores com filtros
        /// </summary>
        public async Task<IEnumerable<ResponseAuthorDTO>> GetAuthorsAsync(AuthorFilterDTO filter)
        {
            var query = _context.Authors
                .Include(a => a.BookAuthors)
                .AsQueryable();

            // ===== FILTROS =====
            
            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var searchTerm = filter.Name.Trim().ToLower();
                query = query.Where(a => a.Name.ToLower().Contains(searchTerm));
            }

            if (filter.BornAfterYear.HasValue)
                query = query.Where(a => a.BirthDate.Year >= filter.BornAfterYear.Value);

            if (filter.BornBeforeYear.HasValue)
                query = query.Where(a => a.BirthDate.Year <= filter.BornBeforeYear.Value);

            if (filter.BirthMonth.HasValue)
                query = query.Where(a => a.BirthDate.Month == filter.BirthMonth.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(a => a.IsActive == filter.IsActive.Value);
            else
                query = query.Where(a => a.IsActive); // Padrão: só ativos

            if (filter.HasBooks.HasValue)
            {
                query = filter.HasBooks.Value
                    ? query.Where(a => a.BookAuthors.Any())
                    : query.Where(a => !a.BookAuthors.Any());
            }

            if (filter.MinBooks.HasValue)
                query = query.Where(a => a.BookAuthors.Count >= filter.MinBooks.Value);

            if (filter.MaxBooks.HasValue)
                query = query.Where(a => a.BookAuthors.Count <= filter.MaxBooks.Value);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(a => a.CreatedAt <= filter.CreatedBefore.Value);

            // ===== ORDENAÇÃO =====
            query = (filter.OrderBy?.ToLower(), filter.OrderDirection?.ToLower()) switch
            {
                ("birthdate", "desc") => query.OrderByDescending(a => a.BirthDate),
                ("birthdate", _) => query.OrderBy(a => a.BirthDate),
                
                ("createdat", "desc") => query.OrderByDescending(a => a.CreatedAt),
                ("createdat", _) => query.OrderBy(a => a.CreatedAt),
                
                ("bookcount", "desc") => query.OrderByDescending(a => a.BookAuthors.Count),
                ("bookcount", _) => query.OrderBy(a => a.BookAuthors.Count),
                
                ("name", "desc") => query.OrderByDescending(a => a.Name),
                _ => query.OrderBy(a => a.Name) // Padrão: name asc
            };

            // ===== PAGINAÇÃO =====
            var authors = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return authors.Select(a => new ResponseAuthorDTO
            {
                Id = a.Id,
                Name = a.Name,
                BirthDate = a.BirthDate,
                IsActive = a.IsActive
            });
        }

        /// <summary>
        /// Criar um novo autor
        /// </summary>
        public async Task<ResponseAuthorDTO> CreateAuthorAsync(CreateAuthorDTO dto)
        {
            // Validar se já existe autor com mesmo nome
            var existingAuthor = await _context.Authors
                .FirstOrDefaultAsync(a => a.Name.ToLower() == dto.Name.ToLower());

            if (existingAuthor != null)
            {
                if (!existingAuthor.IsActive)
                {
                    throw new InvalidOperationException(
                        $"Autor '{dto.Name}' existe mas está inativo. Use o endpoint de restauração.");
                }
                
                throw new InvalidOperationException(
                    $"Autor '{dto.Name}' já está cadastrado (ID: {existingAuthor.Id}).");
            }

            var author = new AutorModelo
            {
                Name = dto.Name,
                BirthDate = dto.BirthDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
                BookAuthors = new List<BookAuthor>()
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Autor criado: {Name} (ID: {Id})", author.Name, author.Id);
            
            return new ResponseAuthorDTO
            {
                Id = author.Id,
                Name = author.Name,
                BirthDate = author.BirthDate,
                IsActive = author.IsActive
            };
        }

        /// <summary>
        /// Atualizar autor existente
        /// </summary>
        public async Task<ResponseAuthorDTO?> UpdateAuthorAsync(int id, UpdateAuthorDTO dto)
        {
            var author = await _context.Authors.FindAsync(id);

            if (author == null)
                return null;

            // Atualizar apenas campos fornecidos (atualização parcial)
            if (dto.Name != null)
            {
                // Validar se novo nome não conflita com outro autor
                var nameExists = await _context.Authors
                    .AnyAsync(a => a.Id != id && 
                                   a.Name.ToLower() == dto.Name.ToLower());
                
                if (nameExists)
                    throw new InvalidOperationException(
                        $"Já existe outro autor com o nome '{dto.Name}'.");
                
                author.Name = dto.Name;
            }

            if (dto.BirthDate.HasValue)
                author.BirthDate = dto.BirthDate.Value;

            if (dto.IsActive.HasValue)
                author.IsActive = dto.IsActive.Value;

            author.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Autor atualizado: {Name} (ID: {Id})", author.Name, author.Id);
            
            return new ResponseAuthorDTO
            {
                Id = author.Id,
                Name = author.Name,
                BirthDate = author.BirthDate,
                IsActive = author.IsActive
            };
        }

        /// <summary>
        /// Soft delete de autor
        /// </summary>
        public async Task<bool> SoftDeleteAuthorAsync(int id)
        {
            var author = await _context.Authors
                .Include(a => a.BookAuthors)
                .ThenInclude(ba => ba.Book)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
                return false;

            // Verificar se há livros ativos com este autor
            var hasActiveBooks = author.BookAuthors
                .Any(ba => ba.Book.IsActive);

            if (hasActiveBooks)
            {
                throw new InvalidOperationException(
                    "Não é possível desativar o autor pois existem livros ativos associados. " +
                    "Desative os livros primeiro ou remova as associações.");
            }

            author.IsActive = false;
            author.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Autor desativado: {Name} (ID: {Id})", author.Name, author.Id);
            
            return true;
        }
    }
}