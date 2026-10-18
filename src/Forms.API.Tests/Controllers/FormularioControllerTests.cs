using Forms.API.Controllers;
using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forms.API.Tests.Controllers;

public class FormularioControllerTests
{
    private readonly Mock<IFormularioService> _formularioServiceMock;
    private readonly FormularioController _controller;

    public FormularioControllerTests()
    {
        _formularioServiceMock = new Mock<IFormularioService>();
        _controller = new FormularioController(_formularioServiceMock.Object);
    }

    [Fact]
    public async Task CreateFormulario_WithValidData_ReturnsCreatedAtActionResult()
    {
        // Arrange
        var formularioDto = new FormularioDTO
        {
            Titulo = "Novo Formulário",
            Descricao = "Descrição do formulário",
            Status = StatusFormulario.Rascunho
        };
        var responseDto = new ResponseDTO<FormularioResponseDTO>
        {
            Status = true,
            Message = "Formulário criado com sucesso.",
            Data = new FormularioResponseDTO
            {
                Id = 10,
                Titulo = "Novo Formulário",
                Descricao = "Descrição do formulário",
                Status = StatusFormulario.Rascunho
            }
        };

        _formularioServiceMock
            .Setup(s => s.AddFormularioAsync(formularioDto))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateFormulario(formularioDto);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(FormularioController.GetFormulario), createdAtResult.ActionName);
        Assert.Equal(10, createdAtResult.RouteValues?["id"]);
        var response = Assert.IsType<ResponseDTO<FormularioResponseDTO>>(createdAtResult.Value);
        Assert.True(response.Status);
        Assert.Equal(10, response.Data?.Id);
        _formularioServiceMock.Verify(s => s.AddFormularioAsync(formularioDto), Times.Once);
    }

    [Fact]
    public async Task GetFormulario_WithExistingId_ReturnsOkResultWithFormulario()
    {
        // Arrange
        int formId = 1;
        var formResponse = new FormularioResponseDTO
        {
            Id = formId,
            Titulo = "Formulário Teste",
            Descricao = "Descrição Teste",
            Status = StatusFormulario.Publicado
        };

        _formularioServiceMock
            .Setup(s => s.GetFormularioByIdAsync(formId))
            .ReturnsAsync(formResponse);

        // Act
        var result = await _controller.GetFormulario(formId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<FormularioResponseDTO>(okResult.Value);
        Assert.Equal(formId, response.Id);
        Assert.Equal("Formulário Teste", response.Titulo);
        _formularioServiceMock.Verify(s => s.GetFormularioByIdAsync(formId), Times.Once);
    }

    [Fact]
    public async Task GetAllFormularios_WhenCalled_ReturnsOkResultWithListOfFormularios()
    {
        // Arrange
        var formulariosList = new List<FormularioResponseDTO>
        {
            new() { Id = 1, Titulo = "Form 1", Status = StatusFormulario.Publicado },
            new() { Id = 2, Titulo = "Form 2", Status = StatusFormulario.Rascunho }
        };

        _formularioServiceMock
            .Setup(s => s.GetAllFormulariosAsync())
            .ReturnsAsync(formulariosList);

        // Act
        var result = await _controller.GetAllFormularios();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<FormularioResponseDTO>>(okResult.Value);
        Assert.Equal(2, list.Count());
        _formularioServiceMock.Verify(s => s.GetAllFormulariosAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateFormulario_WithValidData_ReturnsOkResultWithResponseDTO()
    {
        // Arrange
        int formId = 1;
        var formularioDto = new FormularioDTO
        {
            Titulo = "Formulário Atualizado",
            Status = StatusFormulario.Publicado
        };
        var expectedResponse = new ResponseDTO { Status = true, Message = "Formulário atualizado com sucesso." };

        _formularioServiceMock
            .Setup(s => s.UpdateFormularioAsync(formId, formularioDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateFormulario(formId, formularioDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Formulário atualizado com sucesso.", response.Message);
        _formularioServiceMock.Verify(s => s.UpdateFormularioAsync(formId, formularioDto), Times.Once);
    }

    [Fact]
    public async Task DeleteFormulario_WithValidId_ReturnsOkResultWithResponseDTO()
    {
        // Arrange
        int formId = 1;
        var expectedResponse = new ResponseDTO { Status = true, Message = "Formulário deletado com sucesso." };

        _formularioServiceMock
            .Setup(s => s.DeleteFormularioAsync(formId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteFormulario(formId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Formulário deletado com sucesso.", response.Message);
        _formularioServiceMock.Verify(s => s.DeleteFormularioAsync(formId), Times.Once);
    }
}
