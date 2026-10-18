using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Mappers;
using Forms.API.Models;

namespace Forms.API.Tests.Mappers;

public class FormularioMapperTests
{
    [Fact]
    public void ToModel_MapsDtoToFormularioModelCorrectly()
    {
        // Arrange
        var criador = new UsuarioModel
        {
            Id = 1,
            Nome = "Pedro",
            Email = "pedro@test.com",
            Senha = "pwd",
            Perfil = PerfilUsuario.Comum
        };

        var dto = new FormularioDTO
        {
            Titulo = "  Título Teste  ",
            Descricao = "  Descrição Teste  ",
            Status = StatusFormulario.Publicado,
            Perguntas = new List<PerguntaDTO>
            {
                new() { Enunciado = "P1", Tipo = TipoPergunta.RespostaCurta, Ordem = 1 }
            }
        };

        // Act
        var model = dto.ToModel(criador);

        // Assert
        Assert.Equal("Título Teste", model.Titulo);
        Assert.Equal("Descrição Teste", model.Descricao);
        Assert.Equal(StatusFormulario.Publicado, model.Status);
        Assert.Equal(criador.Id, model.CriadorId);
        Assert.Equal(criador, model.Criador);
        Assert.Single(model.Perguntas);
    }

    [Fact]
    public void ApplyUpdate_UpdatesFieldsCorrectly()
    {
        // Arrange
        var criador = new UsuarioModel
        {
            Id = 1,
            Nome = "Pedro",
            Email = "pedro@test.com",
            Senha = "pwd",
            Perfil = PerfilUsuario.Comum
        };

        var model = new FormularioModel
        {
            Id = 1,
            Titulo = "Título Antigo",
            CriadorId = 1,
            Criador = criador,
            Status = StatusFormulario.Rascunho
        };

        var updateDto = new FormularioDTO
        {
            Titulo = "  Título Novo  ",
            Descricao = "  Nova Descrição  ",
            Status = StatusFormulario.Encerrado
        };

        // Act
        model.ApplyUpdate(updateDto);

        // Assert
        Assert.Equal("Título Novo", model.Titulo);
        Assert.Equal("Nova Descrição", model.Descricao);
        Assert.Equal(StatusFormulario.Encerrado, model.Status);
        Assert.NotNull(model.DataAtualizacao);
    }

    [Fact]
    public void ToResponseDTO_MapsModelToResponseDtoCorrectly()
    {
        // Arrange
        var criador = new UsuarioModel
        {
            Id = 1,
            Nome = "Pedro",
            Email = "pedro@test.com",
            Senha = "pwd",
            Perfil = PerfilUsuario.Comum
        };

        var model = new FormularioModel
        {
            Id = 10,
            Titulo = "Título",
            Descricao = "Desc",
            Status = StatusFormulario.Publicado,
            DataCriacao = DateTime.UtcNow,
            CriadorId = 1,
            Criador = criador
        };

        // Act
        var response = model.ToResponseDTO();

        // Assert
        Assert.Equal(10, response.Id);
        Assert.Equal("Título", response.Titulo);
        Assert.Equal("Desc", response.Descricao);
        Assert.Equal(StatusFormulario.Publicado, response.Status);
        Assert.Empty(response.Perguntas);
    }
}
