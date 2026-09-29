using Forms.API.DTOs;
using Forms.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "UserOnly")]
    public class AlternativaController : ControllerBase
    {
        private readonly IAlternativaService _alternativaService;

        public AlternativaController(IAlternativaService alternativaService)
        {
            _alternativaService = alternativaService;
        }

        #region Alternativas

        [HttpPost]
        public async Task<IActionResult> CreateAlternativa([FromBody] AlternativaDTO alternativa)
        {
            if (alternativa.PerguntaId is null or <= 0)
                return BadRequest(new ResponseDTO { Status = false, Message = "O id da pergunta é obrigatório." });

            ResponseDTO<AlternativaResponseDTO> response = await _alternativaService.AddAlternativaAsync(alternativa.PerguntaId.Value, alternativa);
            return CreatedAtAction(nameof(GetAlternativa), new { id = response.Data!.Id }, response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAlternativa(int id)
        {
            AlternativaResponseDTO alternativa = await _alternativaService.GetAlternativaByIdAsync(id);
            return Ok(alternativa);
        }

        [HttpGet("pergunta/{perguntaId:int}")]
        public async Task<IActionResult> GetAlternativasPorPergunta(int perguntaId)
        {
            IEnumerable<AlternativaResponseDTO> alternativas = await _alternativaService.GetAlternativasByPerguntaIdAsync(perguntaId);
            return Ok(alternativas);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAlternativa(int id, [FromBody] AlternativaDTO alternativa)
        {
            ResponseDTO<AlternativaResponseDTO> response = await _alternativaService.UpdateAlternativaAsync(id, alternativa);
            return Ok(response);
        }

        [HttpPut("pergunta/{perguntaId:int}")]
        public async Task<IActionResult> UpdateAlternativasPorPergunta(int perguntaId, [FromBody] List<AlternativaDTO> alternativas)
        {
            ResponseDTO<IEnumerable<AlternativaResponseDTO>> response = await _alternativaService.UpdateAlternativasPorPerguntaAsync(perguntaId, alternativas);
            return Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAlternativa(int id)
        {
            ResponseDTO response = await _alternativaService.DeleteAlternativaAsync(id);
            return Ok(response);
        }

        #endregion
    }
}
