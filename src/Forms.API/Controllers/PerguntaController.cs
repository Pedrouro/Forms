using Forms.API.DTOs;
using Forms.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "UserOnly")]
    public class PerguntaController : ControllerBase
    {
        private readonly IPerguntaService _perguntaService;

        public PerguntaController(IPerguntaService perguntaService)
        {
            _perguntaService = perguntaService;
        }

        #region Perguntas

        [HttpPost]
        public async Task<IActionResult> CreatePergunta([FromBody] PerguntaDTO pergunta)
        {
            if (pergunta.FormularioId is null or <= 0)
                return BadRequest(new ResponseDTO { Status = false, Message = "O id do formulário é obrigatório." });

            ResponseDTO<PerguntaResponseDTO> response = await _perguntaService.AddPerguntaAsync(pergunta.FormularioId.Value, pergunta);
            return CreatedAtAction(nameof(GetPergunta), new { id = response.Data!.Id }, response);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPergunta(int id)
        {
            PerguntaResponseDTO pergunta = await _perguntaService.GetPerguntaByIdAsync(id);
            return Ok(pergunta);
        }

        [HttpGet("formulario/{formularioId:int}")]
        public async Task<IActionResult> GetPerguntasPorFormulario(int formularioId)
        {
            IEnumerable<PerguntaResponseDTO> perguntas = await _perguntaService.GetPerguntasByFormularioIdAsync(formularioId);
            return Ok(perguntas);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePergunta(int id, [FromBody] PerguntaDTO pergunta)
        {
            ResponseDTO<PerguntaResponseDTO> response = await _perguntaService.UpdatePerguntaAsync(id, pergunta);
            return Ok(response);
        }

        [HttpPut("formulario/{formularioId:int}")]
        public async Task<IActionResult> UpdatePerguntasPorFormulario(int formularioId, [FromBody] List<PerguntaDTO> perguntas)
        {
            ResponseDTO<IEnumerable<PerguntaResponseDTO>> response = await _perguntaService.UpdatePerguntasPorFormularioAsync(formularioId, perguntas);
            return Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePergunta(int id)
        {
            ResponseDTO response = await _perguntaService.DeletePerguntaAsync(id);
            return Ok(response);
        }

        #endregion
    }
}
