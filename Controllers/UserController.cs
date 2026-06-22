using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    [Authorize(Policy = "ClientOnly")]  // ← APENAS CLIENTES!
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ICurrentUserService _currentUser;

        public UsersController(UserService userService, ICurrentUserService currentUser)
        {
            _userService = userService;
            _currentUser = currentUser;
        }
        
        /// <summary>
        /// Registrar novo usuário Público
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<UserResponseDTO>> Register(CreateUserDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = await _userService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetProfile), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Login(público)
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDTO>> Login(LoginDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.LoginAsync(dto);

            if (result == null)
                return Unauthorized(new { message = "Email ou senha inválidos" });

            return Ok(result);
        }

         /// <summary>
        /// Ver perfil do usuário logado (PRIVADO)
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserResponseDTO>> GetProfile()
        {
            
            if (!_currentUser.IsAuthenticated)
                return Unauthorized(new { message = "Usuário não autenticado" });
            var userlog = await _userService.GetUserByIdAsync(_currentUser.UserId!.Value);; // Implementar isso
            
            if (userlog == null)
                return NotFound(new { message = "Usuário não encontrado"});

            return Ok(userlog);
        }

        /// <summary>
        /// Atualizar usuário usuário logado
        /// </summary>
        [HttpPut("profile")]
        [Authorize] // Se colocar "{id}" o usuário pode forçar través de tentativas inserind POST/1 depois 2 etc
        public async Task<ActionResult<UserResponseDTO>> UpdateProfile(UpdateUserDTO dto)
        {
            if (!_currentUser.IsAuthenticated)
                return Unauthorized(new { message = "Usuário não autenticado" });

            try
            {
                var userlog = await _userService.GetUserByIdAsync(_currentUser.UserId!.Value);

                if (userlog == null)
                    return NotFound(new { message = "Usuário não encontrado" });

                return Ok(userlog);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        //// <summary>
        /// Alterar senha do usuário logado (PRIVADO AUTENTICADO)
        /// </summary>
        [HttpPut("profile/change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_currentUser.IsAuthenticated)
                return Unauthorized(new { message = "Usuário não autenticado" });

            try
            {
                await _userService.ChangePasswordAsync(_currentUser.UserId!.Value, dto);
                return Ok(new { message = "Senha alterada com sucesso" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Desativar usuário (soft delete)
        /// </summary>
        [HttpDelete("profile")]
        [Authorize]
        public async Task<ActionResult> DeleteAccount()
        {
            if (!_currentUser.IsAuthenticated)
                return Unauthorized(new { message = "Usuário não autenticado" });

            try
            {
                var result = await _userService.SoftDeleteUserAsync(_currentUser.UserId!.Value);

                if (!result)
                    return NotFound(new { message = "Usuário não encontrado" });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Método auxiliar para pegar o ID do usuário logado
        /// </summary>
        private int? GetCurrentUserId()
        {
            // Versão temporária: pegar do header
            var userIdHeader = Request.Headers["X-User-Id"].FirstOrDefault();
            
            if (int.TryParse(userIdHeader, out int userId))
                return userId;

            // Futuro: pegar do token JWT
            // var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            // return claim != null ? int.Parse(claim.Value) : null;

            return null;
        }
        
    }
}