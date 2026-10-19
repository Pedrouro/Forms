using Forms.API.Controllers;
using Forms.API.DTOs;
using Forms.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Forms.API.Tests.Controllers;

public class AlternativaControllerTests
{
    private readonly Mock<IAlternativaService> _alternativaServiceMock;
    private readonly AlternativaController _controller;

    public AlternativaControllerTests()
    {
        _alternativaServiceMock = new Mock<IAlternativaService>();
        _controller = new AlternativaController(_alternativaServiceMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAlternativa_QuandoPerguntaIdForInvalido_RetornaBadRequest(int? perguntaId)
    {
        // Arrange
        var alternativaDto = new AlternativaDTO
        {
            PerguntaId = perguntaId,
            Texto = "Opção A"
        };

        // Act
        var result = await _controller.CreateAlternativa(alternativaDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(badRequestResult.Value);
        Assert.False(response.Status);
        Assert.Equal("O id da pergunta é obrigatório.", response.Message);
        _alternativaServiceMock.Verify(s => s.AddAlternativaAsync(It.IsAny<int>(), It.IsAny<AlternativaDTO>()), Times.Never);
    }

    [Fact]
    public async Task CreateAlternativa_QuandoValido_RetornaCreatedAtActionResult()
    {
        // Arrange
        var alternativaDto = new AlternativaDTO
        {
            PerguntaId = 1,
            Texto = "Opção 1"
        };
        var responseDto = new ResponseDTO<AlternativaResponseDTO>
        {
            Status = true,
            Message = "Alternativa adicionada com sucesso.",
            Data = new AlternativaResponseDTO
            {
                Id = 10,
                Texto = "Opção 1",
                PerguntaId = 1
            }
        };

        _alternativaServiceMock
            .Setup(s => s.AddAlternativaAsync(1, alternativaDto))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateAlternativa(alternativaDto);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(AlternativaController.GetAlternativa), createdAtResult.ActionName);
        Assert.Equal(10, createdAtResult.RouteValues?["id"]);
        var response = Assert.IsType<ResponseDTO<AlternativaResponseDTO>>(createdAtResult.Value);
        Assert.True(response.Status);
        Assert.Equal(10, response.Data?.Id);
        _alternativaServiceMock.Verify(s => s.AddAlternativaAsync(1, alternativaDto), Times.Once);
    }

    [Fact]
    public async Task GetAlternativa_ComIdExistente_RetornaOkResultComAlternativa()
    {
        // Arrange
        int alternativaId = 10;
        var altResponse = new AlternativaResponseDTO
        {
            Id = alternativaId,
            Texto = "Opção A",
            PerguntaId = 1
        };

        _alternativaServiceMock
            .Setup(s => s.GetAlternativaByIdAsync(alternativaId))
            .ReturnsAsync(altResponse);

        // Act
        var result = await _controller.GetAlternativa(alternativaId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AlternativaResponseDTO>(okResult.Value);
        Assert.Equal(alternativaId, response.Id);
        Assert.Equal("Opção A", response.Texto);
        _alternativaServiceMock.Verify(s => s.GetAlternativaByIdAsync(alternativaId), Times.Once);
    }

    [Fact]
    public async Task GetAlternativasPorPergunta_ComPerguntaIdValido_RetornaOkResultComLista()
    {
        // Arrange
        int perguntaId = 1;
        var lista = new List<AlternativaResponseDTO>
        {
            new() { Id = 1, Texto = "Opção 1", PerguntaId = perguntaId },
            new() { Id = 2, Texto = "Opção 2", PerguntaId = perguntaId }
        };

        _alternativaServiceMock
            .Setup(s => s.GetAlternativasByPerguntaIdAsync(perguntaId))
            .ReturnsAsync(lista);

        // Act
        var result = await _controller.GetAlternativasPorPergunta(perguntaId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsAssignableFrom<IEnumerable<AlternativaResponseDTO>>(okResult.Value);
        Assert.Equal(2, response.Count());
        _alternativaServiceMock.Verify(s => s.GetAlternativasByPerguntaIdAsync(perguntaId), Times.Once);
    }

    [Fact]
    public async Task UpdateAlternativa_ComDadosValidos_RetornaOkResultComResponseDTO()
    {
        // Arrange
        int alternativaId = 10;
        var dto = new AlternativaDTO { Texto = "Novo Texto" };
        var expectedResponse = new ResponseDTO<AlternativaResponseDTO>
        {
            Status = true,
            Message = "Alternativa atualizada com sucesso.",
            Data = new AlternativaResponseDTO { Id = alternativaId, Texto = "Novo Texto" }
        };

        _alternativaServiceMock
            .Setup(s => s.UpdateAlternativaAsync(alternativaId, dto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateAlternativa(alternativaId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO<AlternativaResponseDTO>>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Alternativa atualizada com sucesso.", response.Message);
        _alternativaServiceMock.Verify(s => s.UpdateAlternativaAsync(alternativaId, dto), Times.Once);
    }

    [Fact]
    public async Task UpdateAlternativasPorPergunta_ComDadosValidos_RetornaOkResultComResponseDTO()
    {
        // Arrange
        int perguntaId = 1;
        var lista = new List<AlternativaDTO>
        {
            new() { Id = 1, Texto = "Opção 1 Atualizada" },
            new() { Id = 2, Texto = "Opção 2 Atualizada" }
        };
        var expectedResponse = new ResponseDTO<IEnumerable<AlternativaResponseDTO>>
        {
            Status = true,
            Message = "Alternativas atualizadas com sucesso.",
            Data = new List<AlternativaResponseDTO>
            {
                new() { Id = 1, Texto = "Opção 1 Atualizada" },
                new() { Id = 2, Texto = "Opção 2 Atualizada" }
            }
        };

        _alternativaServiceMock
            .Setup(s => s.UpdateAlternativasPorPerguntaAsync(perguntaId, lista))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateAlternativasPorPergunta(perguntaId, lista);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO<IEnumerable<AlternativaResponseDTO>>>(okResult.Value);
        Assert.True(response.Status);
        _alternativaServiceMock.Verify(s => s.UpdateAlternativasPorPerguntaAsync(perguntaId, lista), Times.Once);
    }

    [Fact]
    public async Task DeleteAlternativa_ComIdValido_RetornaOkResultComResponseDTO()
    {
        // Arrange
        int alternativaId = 10;
        var expectedResponse = new ResponseDTO { Status = true, Message = "Alternativa deletada com sucesso." };

        _alternativaServiceMock
            .Setup(s => s.DeleteAlternativaAsync(alternativaId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteAlternativa(alternativaId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ResponseDTO>(okResult.Value);
        Assert.True(response.Status);
        Assert.Equal("Alternativa deletada com sucesso.", response.Message);
        _alternativaServiceMock.Verify(s => s.DeleteAlternativaAsync(alternativaId), Times.Once);
    }
}
