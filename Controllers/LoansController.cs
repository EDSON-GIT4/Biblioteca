using Microsoft.AspNetCore.Mvc;
using LibraryAPI.DTOs;
using LibraryAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace LibraryAPI.Controllers
{
    [ApiController]
    [Route("/[controller]")]
    [Authorize(Policy = "ClientOnly")]  // ← APENAS CLIENTES!
    public class LoansController : ControllerBase
    {
        private readonly LoanService _loanService;

        public LoansController(LoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpPost]
        public async Task<ActionResult<LoanResponseDTO>> CreateLoan(
            [FromBody] CreateLoanDTO dto)
        {
            try
            {
                var loan = await _loanService.CreateLoanAsync(dto);
                return CreatedAtAction(null, loan);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/return")]
        public async Task<ActionResult<LoanResponseDTO>> ReturnBook(int id)
        {
            try
            {
                var loan = await _loanService.ReturnBookAsync(id);
                return Ok(loan);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<LoanResponseDTO>>> GetActiveLoans()
        {
            var loans = await _loanService.GetActiveLoansAsync();
            return Ok(loans);
        }
    }
}