using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.Extensions.Logging;  

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    public class AuthClientController : ControllerBase
    {
        private readonly ClienteService _clienteService;
        private readonly ILogger<AuthClientController> _logger;

        public AuthClientController(ClienteService clienteService,
            ILogger<AuthClientController> logger)
        {
            _clienteService = clienteService;
            _logger = logger; 
        }

        /// <summary>
        /// Registra um novo cliente da API
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateClienteDTO dto)
        {
            try
            {
                var result = await _clienteService.CreateClienteAsync(dto);
                
                _logger.LogInformation(
                    "Novo cliente registrado: {ClientId}", 
                    result.ClientId);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Autentica um cliente usando Client ID e Secret
        /// </summary>
        [HttpPost("token")]
        public async Task<IActionResult> GetToken(
            [FromHeader(Name = "X-Client-Id")] string clientId,
            [FromHeader(Name = "X-Client-Secret")] string clientSecret)
        {
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                return BadRequest(new 
                { 
                    error = "invalid_request",
                    message = "X-Client-Id e X-Client-Secret são obrigatórios" 
                });
            }

            var result = await _clienteService.AuthenticateClientAsync(
                clientId, clientSecret);

            if (result == null)
            {
                return Unauthorized(new 
                { 
                    error = "invalid_client",
                    message = "Client ID ou Secret inválidos" 
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Revoga o acesso de um cliente
        /// </summary>
        [HttpPost("revoke/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevokeAccess(int id)
        {
            var result = await _clienteService.DeactivateClienteAsync(id);
            
            if (!result)
                return NotFound(new { error = "Cliente não encontrado" });

            return Ok(new { message = "Acesso revogado com sucesso" });
        }
    }
}