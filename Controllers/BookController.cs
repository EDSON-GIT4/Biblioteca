using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    [Authorize(Policy = "ClientOnly")]  // ← APENAS CLIENTES!
    public class BooksController : ControllerBase
    {
        private readonly BookService _bookService;

        public BooksController(BookService bookService)
        {
            _bookService = bookService;
        }

        //Listar books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookResponseDTO>>> GetBooks(
            [FromQuery] BookFilterDTO filter)
        {
            var books = await _bookService.GetBooksAsync(filter);
            return Ok(books);
        }

        //listar book por ID
        [HttpGet("{id}")]
        public async Task<ActionResult<BookResponseDTO>> GetBook(int id)
        {
            var book = await _bookService.GetBookByIdAsync(id);
            
            if (book == null)
                return NotFound(new { message = "Livro não encontrado." });

            return Ok(book);
        }

        //Criar livro
        [HttpPost]
        public async Task<ActionResult<BookResponseDTO>> CreateBook(
            [FromBody] CreateBookDTO dto)
        {
            try
            {
                var book = await _bookService.CreateBookAsync(dto);
                return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            try
            {
                var success = await _bookService.SoftDeleteBookAsync(id);
                
                if (!success)
                    return NotFound(new { message = "Livro não encontrado." });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                    return BadRequest(new { message = ex.Message });
            }
        }
    }
}