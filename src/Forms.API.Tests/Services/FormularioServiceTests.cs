using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Exceptions;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Implementations;
using Forms.API.Services.Interfaces;
using Moq;

namespace Forms.API.Tests.Services;

public class FormularioServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepositoryMock;
    private readonly Mock<IFormularioRepository> _formularioRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly FormularioService _service;

    public FormularioServiceTests()
    {
        _usuarioRepositoryMock = new Mock<IUsuarioRepository>();
        _formularioRepositoryMock = new Mock<IFormularioRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new FormularioService(
            _usuarioRepositoryMock.Object,
            _formularioRepositoryMock.Object,
            _currentUserServiceMock.Object
        );
    }

    private UsuarioModel CreateUser(int id = 1, PerfilUsuario perfil = PerfilUsuario.Comum) =>
        new()
        {
            Id = id,
            Nome = "Test User",
            Email = "user@test.com",
            Senha = "hash",
            Perfil = perfil
        };

    private FormularioModel CreateFormulario(int id = 1, int criadorId = 1, StatusFormulario status = StatusFormulario.Rascunho)
    {
        var criador = CreateUser(criadorId);
        return new FormularioModel
        {
            Id = id,
            Titulo = "Formulário de Pesquisa",
            Descricao = "Descrição",
            CriadorId = criadorId,
            Criador = criador,
            Status = status
        };
    }

    [Fact]
    public async Task AddFormularioAsync_QuandoUsuarioNaoEncontrado_LancaNotFoundException()
    {
        // Arrange
        int userId = 99;
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _usuarioRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((UsuarioModel)null!);

        var dto = new FormularioDTO { Titulo = "Formulário 1" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.AddFormularioAsync(dto));
        Assert.Equal($"Usuário com id {userId} não encontrado.", ex.Message);
        _formularioRepositoryMock.Verify(r => r.AddAsync(It.IsAny<FormularioModel>()), Times.Never);
    }

    [Fact]
    public async Task AddFormularioAsync_QuandoPerguntaNaoPermitirAlternativasMasPossuir_LancaArgumentException()
    {
        // Arrange
        int userId = 1;
        var user = CreateUser(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _usuarioRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var dto = new FormularioDTO
        {
            Titulo = "Formulário Inválido",
            Perguntas = new List<PerguntaDTO>
            {
                new()
                {
                    Enunciado = "Digite seu nome",
                    Tipo = TipoPergunta.RespostaCurta,
                    Alternativas = new List<AlternativaDTO> { new() { Texto = "Invalido" } }
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddFormularioAsync(dto));
        Assert.Contains("não aceitam alternativas", ex.Message);
        _formularioRepositoryMock.Verify(r => r.AddAsync(It.IsAny<FormularioModel>()), Times.Never);
    }

    [Fact]
    public async Task AddFormularioAsync_QuandoTextoDaAlternativaEstiverVazio_LancaArgumentException()
    {
        // Arrange
        int userId = 1;
        var user = CreateUser(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _usuarioRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var dto = new FormularioDTO
        {
            Titulo = "Formulário Múltipla Escolha",
            Perguntas = new List<PerguntaDTO>
            {
                new()
                {
                    Enunciado = "Escolha uma cor",
                    Tipo = TipoPergunta.MultiplaEscolha,
                    Alternativas = new List<AlternativaDTO> { new() { Texto = "   " } }
                }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddFormularioAsync(dto));
        Assert.Equal("O texto da alternativa não pode ser vazio.", ex.Message);
        _formularioRepositoryMock.Verify(r => r.AddAsync(It.IsAny<FormularioModel>()), Times.Never);
    }

    [Fact]
    public async Task AddFormularioAsync_QuandoValido_AdicionaFormularioERetornaResponseDTO()
    {
        // Arrange
        int userId = 1;
        var user = CreateUser(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _usuarioRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var dto = new FormularioDTO
        {
            Titulo = "Pesquisa de Clima Organizacional",
            Descricao = "Pesquisa semestral",
            Status = StatusFormulario.Rascunho
        };

        _formularioRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<FormularioModel>()))
            .ReturnsAsync((FormularioModel f) => f);

        // Act
        var result = await _service.AddFormularioAsync(dto);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Formulário criado com sucesso.", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(dto.Titulo, result.Data.Titulo);
        _formularioRepositoryMock.Verify(r => r.AddAsync(It.Is<FormularioModel>(f => f.Titulo == dto.Titulo && f.CriadorId == userId)), Times.Once);
    }

    [Fact]
    public async Task GetFormularioByIdAsync_QuandoFormularioNaoEncontrado_LancaNotFoundException()
    {
        // Arrange
        int formId = 1;
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync((FormularioModel?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetFormularioByIdAsync(formId));
        Assert.Equal($"Formulário com id {formId} não encontrado.", ex.Message);
    }

    [Fact]
    public async Task GetFormularioByIdAsync_QuandoNaoPublicadoEUsuarioNaoForDonoNemAdmin_LancaNotFoundException()
    {
        // Arrange
        int formId = 1;
        int criadorId = 10;
        int currentUserId = 20;
        var formulario = CreateFormulario(formId, criadorId, StatusFormulario.Rascunho);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(formulario);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetFormularioByIdAsync(formId));
        Assert.Equal($"Formulário com id {formId} não encontrado.", ex.Message);
    }

    [Fact]
    public async Task GetFormularioByIdAsync_QuandoFormularioEstiverPublicado_RetornaFormularioMesmoNaoSendoDono()
    {
        // Arrange
        int formId = 1;
        int criadorId = 10;
        int currentUserId = 20;
        var formulario = CreateFormulario(formId, criadorId, StatusFormulario.Publicado);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(formulario);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act
        var result = await _service.GetFormularioByIdAsync(formId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(formId, result.Id);
    }

    [Fact]
    public async Task GetFormularioByIdAsync_QuandoUsuarioForDono_RetornaFormulario()
    {
        // Arrange
        int formId = 1;
        int criadorId = 10;
        var formulario = CreateFormulario(formId, criadorId, StatusFormulario.Rascunho);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(formulario);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(criadorId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act
        var result = await _service.GetFormularioByIdAsync(formId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(formId, result.Id);
    }

    [Fact]
    public async Task GetAllFormulariosAsync_QuandoChamado_RetornaTodosOsFormulariosMapeados()
    {
        // Arrange
        var list = new List<FormularioModel>
        {
            CreateFormulario(1, 1),
            CreateFormulario(2, 1)
        };

        _formularioRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(list);

        // Act
        var result = await _service.GetAllFormulariosAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateFormularioAsync_QuandoNaoEncontrado_LancaNotFoundException()
    {
        // Arrange
        int formId = 1;
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync((FormularioModel?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateFormularioAsync(formId, new FormularioDTO { Titulo = "T" }));
        Assert.Equal($"Formulário com id {formId} não encontrado.", ex.Message);
    }

    [Fact]
    public async Task UpdateFormularioAsync_QuandoUsuarioNaoAutorizado_LancaForbiddenException()
    {
        // Arrange
        int formId = 1;
        var formulario = CreateFormulario(formId, criadorId: 10);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(formulario);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(20);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateFormularioAsync(formId, new FormularioDTO { Titulo = "T" }));
        Assert.Equal("Você não tem permissão para alterar este formulário.", ex.Message);
        _formularioRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FormularioModel>()), Times.Never);
    }

    [Fact]
    public async Task UpdateFormularioAsync_QuandoUsuarioForDono_AtualizaFormularioComSucesso()
    {
        // Arrange
        int formId = 1;
        int userId = 10;
        var formulario = CreateFormulario(formId, criadorId: userId);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(formulario);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);
        _formularioRepositoryMock.Setup(r => r.UpdateAsync(formulario)).Returns(Task.CompletedTask);

        var dto = new FormularioDTO
        {
            Titulo = "Título Atualizado",
            Descricao = "Nova Descrição",
            Status = StatusFormulario.Publicado
        };

        // Act
        var result = await _service.UpdateFormularioAsync(formId, dto);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Formulário atualizado com sucesso.", result.Message);
        Assert.Equal("Título Atualizado", formulario.Titulo);
        Assert.Equal(StatusFormulario.Publicado, formulario.Status);
        _formularioRepositoryMock.Verify(r => r.UpdateAsync(formulario), Times.Once);
    }

    [Fact]
    public async Task DeleteFormularioAsync_QuandoNaoEncontrado_LancaNotFoundException()
    {
        // Arrange
        int formId = 1;
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync((FormularioModel?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteFormularioAsync(formId));
        Assert.Equal($"Formulário com id {formId} não encontrado.", ex.Message);
    }

    [Fact]
    public async Task DeleteFormularioAsync_QuandoUsuarioNaoAutorizado_LancaForbiddenException()
    {
        // Arrange
        int formId = 1;
        var formulario = CreateFormulario(formId, criadorId: 10);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(formulario);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(20);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _service.DeleteFormularioAsync(formId));
        Assert.Equal("Você não tem permissão para deletar este formulário.", ex.Message);
        _formularioRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<FormularioModel>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFormularioAsync_QuandoUsuarioForAdmin_DeletaComSucesso()
    {
        // Arrange
        int formId = 1;
        var formulario = CreateFormulario(formId, criadorId: 10);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(formulario);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(999);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Administrador);
        _formularioRepositoryMock.Setup(r => r.DeleteAsync(formulario)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteFormularioAsync(formId);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Formulário deletado com sucesso.", result.Message);
        _formularioRepositoryMock.Verify(r => r.DeleteAsync(formulario), Times.Once);
    }
}
