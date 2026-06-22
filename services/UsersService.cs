using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LibraryAPI.Data;
using LibraryAPI.DTOs;
using LibraryAPI.Helpers;
using LibraryAPI.Model;

namespace LibraryAPI.Services
{
    public class UserService
    {
        private readonly LibraryDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly AuthServiceUser _authService;

        public UserService(
            LibraryDbContext context, 
            ILogger<UserService> logger,
            AuthServiceUser authService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
        }

        /// <summary>
        /// Buscar usuário por ID
        /// </summary>
        public async Task<UserResponseDTO?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Loans)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

            if (user == null)
                return null;

            return MapToResponseDTO(user);
        }

        /// <summary>
        /// Criar novo usuário
        /// </summary>
        public async Task<UserResponseDTO> CreateUserAsync(CreateUserDTO dto)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (emailExists)
                throw new InvalidOperationException($"Email '{dto.Email}' já está cadastrado.");

            var user = new UsuarioModelo
            {
                Name = dto.Name,
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                RegistrationDate = DateTime.UtcNow,
                IsActive = true,
                MaxLoansAllowed = 3,
                Loans = new List<Loan>()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário criado: {Name} (ID: {Id})", user.Name, user.Id);

            return MapToResponseDTO(user);
        }

        /// <summary>
        /// Login com retorno de token JWT
        /// </summary>
        public async Task<LoginResponseDTO?> LoginAsync(LoginDTO dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null || !user.IsActive)
                return null;

            if (!PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                return null;

            _logger.LogInformation("Login bem-sucedido: {Email}", user.Email);

            // Gerar token JWT
            var token = _authService.GenerateJwtToken(user.Id, user.Email, user.Name);

            return new LoginResponseDTO
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        /// <summary>
        /// Atualizar usuário
        /// </summary>
        public async Task<UserResponseDTO?> UpdateUserAsync(int id, UpdateUserDTO dto)
        {
            var user = await _context.Users
                .Include(u => u.Loans)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return null;

            if (dto.Name != null)
                user.Name = dto.Name;

            if (dto.Email != null)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Id != id && 
                                   u.Email.ToLower() == dto.Email.ToLower());

                if (emailExists)
                    throw new InvalidOperationException($"Email '{dto.Email}' já está em uso.");

                user.Email = dto.Email.ToLower().Trim();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Usuário atualizado: {Name} (ID: {Id})", user.Name, id);

            return MapToResponseDTO(user);
        }

        /// <summary>
        /// Alterar senha
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int id, ChangePasswordDTO dto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                throw new InvalidOperationException("Usuário não encontrado.");

            if (!PasswordHasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new InvalidOperationException("Senha atual incorreta.");

            user.PasswordHash = PasswordHasher.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Senha alterada para usuário ID: {Id}", id);
            return true;
        }

        /// <summary>
        /// Soft delete
        /// </summary>
        public async Task<bool> SoftDeleteUserAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Loans)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return false;

            var hasActiveLoans = user.Loans.Any(l => l.Status == LoanStatus.Active);
            if (hasActiveLoans)
                throw new InvalidOperationException(
                    "Não é possível desativar conta com empréstimos ativos.");

            user.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Usuário desativou conta: {Name} (ID: {Id})", user.Name, id);
            return true;
        }

        private static UserResponseDTO MapToResponseDTO(UsuarioModelo user)
        {
            var activeLoans = user.Loans?.Count(l => l.Status == LoanStatus.Active) ?? 0;
            
            return new UserResponseDTO
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                RegistrationDate = user.RegistrationDate,
                IsActive = user.IsActive,
                MaxLoansAllowed = user.MaxLoansAllowed,
                CurrentLoansCount = activeLoans,
                AvailableLoans = user.MaxLoansAllowed - activeLoans
            };
        }
    }
}