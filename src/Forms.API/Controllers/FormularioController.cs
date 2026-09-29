using Forms.API.DTOs;
using Forms.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forms.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FormularioController : ControllerBase
    {
        private readonly IFormularioService _formularioService;

        public FormularioController(IFormularioService formularioService)
        {
            _formularioService = formularioService;
        }

        #region Formulários

        [Authorize(Policy = "UserOnly")]
        [HttpPost]
        public async Task<IActionResult> CreateFormulario([FromBody] FormularioDTO formulario)
        {
            ResponseDTO<FormularioResponseDTO> response = await _formularioService.AddFormularioAsync(formulario);
            return CreatedAtAction(nameof(GetFormulario), new { id = response.Data!.Id }, response);
        }

        [Authorize(Policy = "UserOnly")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetFormulario(int id)
        {
            FormularioResponseDTO formulario = await _formularioService.GetFormularioByIdAsync(id);
            return Ok(formulario);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public async Task<IActionResult> GetAllFormularios()
        {
            IEnumerable<FormularioResponseDTO> formularios = await _formularioService.GetAllFormulariosAsync();
            return Ok(formularios);
        }

        [Authorize(Policy = "UserOnly")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateFormulario(int id, [FromBody] FormularioDTO formulario)
        {
            ResponseDTO response = await _formularioService.UpdateFormularioAsync(id, formulario);
            return Ok(response);
        }

        [Authorize(Policy = "UserOnly")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteFormulario(int id)
        {
            ResponseDTO response = await _formularioService.DeleteFormularioAsync(id);
            return Ok(response);
        }

        #endregion
    }
}