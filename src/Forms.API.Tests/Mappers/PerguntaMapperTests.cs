using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Mappers;
using Forms.API.Models;

namespace Forms.API.Tests.Mappers;

public class PerguntaMapperTests
{
    [Fact]
    public void ToModel_MapeiaDtoParaPerguntaModelCorretamente()
    {
        // Arrange
        var dto = new PerguntaDTO
        {
            Enunciado = "  Qual sua linguagem favorita?  ",
            Tipo = TipoPergunta.MultiplaEscolha,
            Obrigatoria = true,
            Alternativas = new List<AlternativaDTO>
            {
                new() { Texto = "  C#  " },
                new() { Texto = "  TypeScript  " }
            }
        };

        // Act
        var model = dto.ToModel(formularioId: 5, ordem: 2);

        // Assert
        Assert.Equal("Qual sua linguagem favorita?", model.Enunciado);
        Assert.Equal(TipoPergunta.MultiplaEscolha, model.Tipo);
        Assert.Equal(2, model.Ordem);
        Assert.True(model.Obrigatoria);
        Assert.Equal(5, model.FormularioId);
        Assert.Equal(2, model.Alternativas.Count);
        Assert.Equal("C#", model.Alternativas.First().Texto);
    }

    [Fact]
    public void ApplyUpdate_AtualizaCamposCorretamente()
    {
        // Arrange
        var model = new PerguntaModel
        {
            Id = 1,
            Enunciado = "Enunciado Antigo",
            Tipo = TipoPergunta.RespostaCurta,
            Ordem = 1,
            Obrigatoria = false,
            FormularioId = 5
        };

        var updateDto = new PerguntaDTO
        {
            Enunciado = "  Enunciado Novo  ",
            Tipo = TipoPergunta.TextoLongo,
            Ordem = 3,
            Obrigatoria = true
        };

        // Act
        model.ApplyUpdate(updateDto);

        // Assert
        Assert.Equal("Enunciado Novo", model.Enunciado);
        Assert.Equal(TipoPergunta.TextoLongo, model.Tipo);
        Assert.Equal(3, model.Ordem);
        Assert.True(model.Obrigatoria);
    }

    [Fact]
    public void ToResponseDTO_MapeiaModelParaResponseDtoCorretamente()
    {
        // Arrange
        var model = new PerguntaModel
        {
            Id = 1,
            Enunciado = "Pergunta",
            Tipo = TipoPergunta.RespostaCurta,
            Ordem = 1,
            Obrigatoria = true,
            FormularioId = 10,
            Alternativas = new List<AlternativaModel>()
        };

        // Act
        var response = model.ToResponseDTO();

        // Assert
        Assert.Equal(1, response.Id);
        Assert.Equal("Pergunta", response.Enunciado);
        Assert.Equal(TipoPergunta.RespostaCurta, response.Tipo);
        Assert.Equal(1, response.Ordem);
        Assert.True(response.Obrigatoria);
        Assert.Equal(10, response.FormularioId);
        Assert.Empty(response.Alternativas);
    }
}
