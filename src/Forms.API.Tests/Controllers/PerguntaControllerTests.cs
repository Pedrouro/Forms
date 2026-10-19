using Forms.API.Controllers;
using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forms.API.Tests.Controllers;

public class PerguntaControllerTests
{
    private readonly Mock<IPerguntaService> _perguntaServiceMock;
    private readonly PerguntaController _controller;

    public PerguntaControllerTests()
    {
        _perguntaServiceMock = new Mock<IPerguntaService>();
        _controller = new PerguntaController(_perguntaServiceMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreatePergunta_QuandoFormularioIdForInvalido_RetornaBadRequest(int? formularioId)
    {
        // Arrange
        var perguntaDto = new PerguntaDTO
        {
            FormularioId = formularioId,
            Enunciado = "Qual sua idade?",
            Tipo = TipoPergunta.RespostaCurta
        };

        // Act
        var result = await _controller.CreatePergunta(perguntaDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(badRequestResult.Value);
        Assert.False(response.Status);
        Assert.Equal("O id do formulário é obrigatório.", response.Message);
        _perguntaServiceMock.Verify(s => s.AddPerguntaAsync(It.IsAny<int>(), It.IsAny<PerguntaDTO>()), Times.Never);
    }

    [Fact]
    public async Task CreatePergunta_QuandoValido_RetornaCreatedAtActionResult()
    {
        // Arrange
        var perguntaDto = new PerguntaDTO
        {
            FormularioId = 1,
            Enunciado = "Qual seu nome?",
            Tipo = TipoPergunta.RespostaCurta
        };
        var responseDto = new ResponseDTO<PerguntaResponseDTO>
        {
            Status = true,
            Message = "Pergunta adicionada com sucesso.",
            Data = new PerguntaResponseDTO
            {
                Id = 5,
                Enunciado = "Qual seu nome?",
                Tipo = TipoPergunta.RespostaCurta,
                FormularioId = 1
            }
        };

        _perguntaServiceMock
            .Setup(s => s.AddPerguntaAsync(1, perguntaDto))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreatePergunta(perguntaDto);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PerguntaController.GetPergunta), createdAtResult.ActionName);
        Assert.Equal(5, createdAtResult.RouteValues?["id"]);
        var response = Assert.IsType<ResponseDTO<PerguntaResponseDTO>>(createdAtResult.Value);
        Assert.True(response.Status);
        Assert.Equal(5, response.Data?.Id);
        _perguntaServiceMock.Verify(s => s.AddPerguntaAsync(1, perguntaDto), Times.Once);
    }

    [Fact]
    public async Task GetPergunta_ComIdExistente_RetornaOkResultComPergunta()
    {
        // Arrange
        int perguntaId = 5;
        var perguntaResponse = new PerguntaResponseDTO
        {
            Id = perguntaId,
            Enunciado = "Qual sua cor favorita?",
            Tipo = TipoPergunta.RespostaCurta,
            FormularioId = 1
        };

        _perguntaServiceMock
            .Setup(s => s.GetPerguntaByIdAsync(perguntaId))
            .ReturnsAsync(perguntaResponse);

        // Act
        var result = await _controller.GetPergunta(perguntaId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PerguntaResponseDTO>(okResult.Value);
        Assert.Equal(perguntaId, response.Id);
        Assert.Equal("Qual sua cor favorita?", response.Enunciado);
        _perguntaServiceMock.Verify(s => s.GetPerguntaByIdAsync(perguntaId), Times.Once);
    }

    [Fact]
    public async Task GetPerguntasPorFormulario_ComFormularioIdValido_RetornaOkResultComLista()
    {
        // Arrange
        int formularioId = 1;
        var perguntasList = new List<PerguntaResponseDTO>
        {
            new() { Id = 1, Enunciado = "Pergunta 1", FormularioId = formularioId },
            new() { Id = 2, Enunciado = "Pergunta 2", FormularioId = formularioId }
        };

        _perguntaServiceMock
            .Setup(s => s.GetPerguntasByFormularioIdAsync(formularioId))
            .ReturnsAsync(perguntasList);

        // Act
        var result = await _controller.GetPerguntasPorFormulario(formularioId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<PerguntaResponseDTO>>(okResult.Value);
        Assert.Equal(2, response.Count());
        _perguntaServiceMock.Verify(s => s.GetPerguntasByFormularioIdAsync(formularioId), Times.Once);
    }

    [Fact]
    public async Task UpdatePergunta_ComDadosValidos_RetornaOkResultComResponseDTO()
    {
        // Arrange
        int perguntaId = 5;
        var perguntaDto = new PerguntaDTO
        {
            Enunciado = "Enunciado Atualizado",
            Tipo = TipoPergunta.TextoLongo
        };
        var expectedResponse = new ResponseDTO<PerguntaResponseDTO>
        {
            Status = true,
            Message = "Pergunta atualizada com sucesso.",
            Data = new PerguntaResponseDTO { Id = perguntaId, Enunciado = "Enunciado Atualizado" }
        };

        _perguntaServiceMock
            .Setup(s => s.UpdatePerguntaAsync(perguntaId, perguntaDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdatePergunta(perguntaId, perguntaDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO<PerguntaResponseDTO>>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Pergunta atualizada com sucesso.", response.Message);
        _perguntaServiceMock.Verify(s => s.UpdatePerguntaAsync(perguntaId, perguntaDto), Times.Once);
    }

    [Fact]
    public async Task UpdatePerguntasPorFormulario_ComDadosValidos_RetornaOkResultComResponseDTO()
    {
        // Arrange
        int formularioId = 1;
        var listaPerguntas = new List<PerguntaDTO>
        {
            new() { Id = 1, Enunciado = "P1 Atualizada", Tipo = TipoPergunta.RespostaCurta },
            new() { Id = 2, Enunciado = "P2 Atualizada", Tipo = TipoPergunta.RespostaCurta }
        };
        var expectedResponse = new ResponseDTO<IEnumerable<PerguntaResponseDTO>>
        {
            Status = true,
            Message = "Perguntas atualizadas com sucesso.",
            Data = new List<PerguntaResponseDTO>
            {
                new() { Id = 1, Enunciado = "P1 Atualizada" },
                new() { Id = 2, Enunciado = "P2 Atualizada" }
            }
        };

        _perguntaServiceMock
            .Setup(s => s.UpdatePerguntasPorFormularioAsync(formularioId, listaPerguntas))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdatePerguntasPorFormulario(formularioId, listaPerguntas);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO<IEnumerable<PerguntaResponseDTO>>>(okResult.Value);
        Assert.True(response.Status);
        _perguntaServiceMock.Verify(s => s.UpdatePerguntasPorFormularioAsync(formularioId, listaPerguntas), Times.Once);
    }

    [Fact]
    public async Task DeletePergunta_ComIdValido_RetornaOkResultComResponseDTO()
    {
        // Arrange
        int perguntaId = 5;
        var expectedResponse = new ResponseDTO { Status = true, Message = "Pergunta deletada com sucesso." };

        _perguntaServiceMock
            .Setup(s => s.DeletePerguntaAsync(perguntaId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeletePergunta(perguntaId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Pergunta deletada com sucesso.", response.Message);
        _perguntaServiceMock.Verify(s => s.DeletePerguntaAsync(perguntaId), Times.Once);
    }
}
