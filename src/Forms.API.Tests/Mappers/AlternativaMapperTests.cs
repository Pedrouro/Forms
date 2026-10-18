using Forms.API.DTOs;
using Forms.API.Mappers;
using Forms.API.Models;

namespace Forms.API.Tests.Mappers;

public class AlternativaMapperTests
{
    [Fact]
    public void ToModel_MapsDtoToAlternativaModelCorrectly()
    {
        // Arrange
        var dto = new AlternativaDTO { Texto = "  Alternativa 1  " };

        // Act
        var model = dto.ToModel(perguntaId: 3);

        // Assert
        Assert.Equal("Alternativa 1", model.Texto);
        Assert.Equal(3, model.PerguntaId);
    }

    [Fact]
    public void ApplyUpdate_UpdatesTextoCorrectly()
    {
        // Arrange
        var model = new AlternativaModel
        {
            Id = 1,
            Texto = "Texto Antigo",
            PerguntaId = 3
        };

        var updateDto = new AlternativaDTO { Texto = "  Texto Novo  " };

        // Act
        model.ApplyUpdate(updateDto);

        // Assert
        Assert.Equal("Texto Novo", model.Texto);
    }

    [Fact]
    public void ToResponseDTO_MapsModelToResponseDtoCorrectly()
    {
        // Arrange
        var model = new AlternativaModel
        {
            Id = 7,
            Texto = "Alternativa 7",
            PerguntaId = 3
        };

        // Act
        var response = model.ToResponseDTO();

        // Assert
        Assert.Equal(7, response.Id);
        Assert.Equal("Alternativa 7", response.Texto);
        Assert.Equal(3, response.PerguntaId);
    }
}
