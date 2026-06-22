using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;


namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    [Authorize(Policy = "ClientOnly")]  // ← APENAS CLIENTES!
    public class AuthorsController : ControllerBase
    {
        private readonly AuthorService _authorService;

        public AuthorsController(AuthorService authorService)
        {
            _authorService = authorService;
        }

        /// <summary>
        /// Listar autores com filtros
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResponseAuthorDTO>>> GetAuthors(
            [FromQuery] AuthorFilterDTO filter)
        {
            var authors = await _authorService.GetAuthorsAsync(filter);
            return Ok(authors);
        }

        /// <summary>
        /// Buscar autor por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseAuthorDTO>> GetAuthor(int id)
        {
            var author = await _authorService.GetAuthorByIdAsync(id);
            
            if (author == null)
                return NotFound(new { message = "Autor não encontrado" });
            
            return Ok(author);
        }

        /// <summary>
        /// Criar novo autor
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ResponseAuthorDTO>> CreateAuthor(CreateAuthorDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var author = await _authorService.CreateAuthorAsync(dto);
                return CreatedAtAction(nameof(GetAuthor), new { id = author.Id }, author);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Atualizar autor
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseAuthorDTO>> UpdateAuthor(
            int id, UpdateAuthorDTO dto)
        {
            try
            {
                var author = await _authorService.UpdateAuthorAsync(id, dto);
                
                if (author == null)
                    return NotFound(new { message = "Autor não encontrado" });
                
                return Ok(author);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Desativar autor (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuthor(int id)
        {
            try
            {
                var result = await _authorService.SoftDeleteAuthorAsync(id);
                
                if (!result)
                    return NotFound(new { message = "Autor não encontrado" });
                
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}