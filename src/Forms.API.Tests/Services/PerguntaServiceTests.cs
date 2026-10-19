using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Exceptions;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Implementations;
using Forms.API.Services.Interfaces;
using Moq;

namespace Forms.API.Tests.Services;

public class PerguntaServiceTests
{
    private readonly Mock<IPerguntaRepository> _perguntaRepositoryMock;
    private readonly Mock<IFormularioRepository> _formularioRepositoryMock;
    private readonly Mock<IAlternativaRepository> _alternativaRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly PerguntaService _service;

    public PerguntaServiceTests()
    {
        _perguntaRepositoryMock = new Mock<IPerguntaRepository>();
        _formularioRepositoryMock = new Mock<IFormularioRepository>();
        _alternativaRepositoryMock = new Mock<IAlternativaRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new PerguntaService(
            _perguntaRepositoryMock.Object,
            _formularioRepositoryMock.Object,
            _alternativaRepositoryMock.Object,
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
            Titulo = "Formulário Teste",
            CriadorId = criadorId,
            Criador = criador,
            Status = status
        };
    }

    private PerguntaModel CreatePergunta(int id = 1, int formularioId = 1, TipoPergunta tipo = TipoPergunta.RespostaCurta, int ordem = 1) =>
        new()
        {
            Id = id,
            Enunciado = "Qual sua idade?",
            Tipo = tipo,
            Ordem = ordem,
            FormularioId = formularioId
        };

    [Fact]
    public async Task AddPerguntaAsync_QuandoFormularioNaoEncontrado_LancaNotFoundException()
    {
        // Arrange
        int formId = 1;
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync((FormularioModel?)null);

        var dto = new PerguntaDTO { Enunciado = "Pergunta", Tipo = TipoPergunta.RespostaCurta };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.AddPerguntaAsync(formId, dto));
        Assert.Equal($"Formulário com id {formId} não encontrado.", ex.Message);
    }

    [Fact]
    public async Task AddPerguntaAsync_QuandoUsuarioNaoAutorizado_LancaForbiddenException()
    {
        // Arrange
        int formId = 1;
        var form = CreateFormulario(formId, criadorId: 10);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(20);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        var dto = new PerguntaDTO { Enunciado = "Pergunta", Tipo = TipoPergunta.RespostaCurta };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _service.AddPerguntaAsync(formId, dto));
        Assert.Equal("Você não tem permissão para alterar este formulário.", ex.Message);
    }

    [Fact]
    public async Task AddPerguntaAsync_QuandoFormularioEncerrado_LancaArgumentException()
    {
        // Arrange
        int formId = 1;
        int userId = 10;
        var form = CreateFormulario(formId, criadorId: userId, status: StatusFormulario.Encerrado);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        var dto = new PerguntaDTO { Enunciado = "Pergunta", Tipo = TipoPergunta.RespostaCurta };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddPerguntaAsync(formId, dto));
        Assert.Equal("Não é possível modificar um formulário encerrado.", ex.Message);
    }

    [Fact]
    public async Task AddPerguntaAsync_QuandoTipoNaoPermitirAlternativasMasPossuir_LancaArgumentException()
    {
        // Arrange
        int formId = 1;
        int userId = 10;
        var form = CreateFormulario(formId, criadorId: userId);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        var dto = new PerguntaDTO
        {
            Enunciado = "Texto Longo",
            Tipo = TipoPergunta.TextoLongo,
            Alternativas = new List<AlternativaDTO> { new() { Texto = "Opção" } }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddPerguntaAsync(formId, dto));
        Assert.Contains("não aceitam alternativas", ex.Message);
    }

    [Fact]
    public async Task AddPerguntaAsync_QuandoValido_AdicionaPerguntaEAtualizaFormulario()
    {
        // Arrange
        int formId = 1;
        int userId = 10;
        var form = CreateFormulario(formId, criadorId: userId);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);
        _perguntaRepositoryMock.Setup(r => r.GetMaxOrdemByFormularioIdAsync(formId)).ReturnsAsync(2);
        _perguntaRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PerguntaModel>())).ReturnsAsync((PerguntaModel p) => p);
        _formularioRepositoryMock.Setup(r => r.UpdateAsync(form)).Returns(Task.CompletedTask);

        var dto = new PerguntaDTO
        {
            Enunciado = "Qual seu cargo?",
            Tipo = TipoPergunta.RespostaCurta,
            Ordem = 0
        };

        // Act
        var result = await _service.AddPerguntaAsync(formId, dto);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Pergunta adicionada com sucesso.", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.Ordem); // max (2) + 1
        _perguntaRepositoryMock.Verify(r => r.AddAsync(It.Is<PerguntaModel>(p => p.Enunciado == dto.Enunciado && p.Ordem == 3)), Times.Once);
        _formularioRepositoryMock.Verify(r => r.UpdateAsync(form), Times.Once);
    }

    [Fact]
    public async Task GetPerguntaByIdAsync_QuandoPerguntaNaoEncontrada_LancaNotFoundException()
    {
        // Arrange
        int perguntaId = 99;
        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync((PerguntaModel?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetPerguntaByIdAsync(perguntaId));
        Assert.Equal($"Pergunta com id {perguntaId} não encontrada.", ex.Message);
    }

    [Fact]
    public async Task GetPerguntaByIdAsync_QuandoValido_RetornaPerguntaResponse()
    {
        // Arrange
        int perguntaId = 1;
        int formId = 10;
        int userId = 5;
        var pergunta = CreatePergunta(perguntaId, formId);
        var form = CreateFormulario(formId, criadorId: userId, status: StatusFormulario.Publicado);

        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act
        var result = await _service.GetPerguntaByIdAsync(perguntaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(perguntaId, result.Id);
        Assert.Equal(pergunta.Enunciado, result.Enunciado);
    }

    [Fact]
    public async Task GetPerguntasByFormularioIdAsync_QuandoValido_RetornaListaDePerguntas()
    {
        // Arrange
        int formId = 10;
        int userId = 5;
        var form = CreateFormulario(formId, criadorId: userId);
        var perguntas = new List<PerguntaModel>
        {
            CreatePergunta(1, formId, ordem: 1),
            CreatePergunta(2, formId, ordem: 2)
        };

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);
        _perguntaRepositoryMock.Setup(r => r.GetByFormularioIdAsync(formId)).ReturnsAsync(perguntas);

        // Act
        var result = await _service.GetPerguntasByFormularioIdAsync(formId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdatePerguntaAsync_QuandoAlterarTipoParaSemAlternativas_LimpaAlternativas()
    {
        // Arrange
        int perguntaId = 1;
        int formId = 10;
        int userId = 5;
        var pergunta = CreatePergunta(perguntaId, formId, TipoPergunta.MultiplaEscolha);
        var alt1 = new AlternativaModel { Id = 1, Texto = "Opção 1", PerguntaId = perguntaId };
        pergunta.Alternativas.Add(alt1);

        var form = CreateFormulario(formId, criadorId: userId);

        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);
        _alternativaRepositoryMock.Setup(r => r.DeleteAsync(alt1)).Returns(Task.CompletedTask);
        _perguntaRepositoryMock.Setup(r => r.UpdateAsync(pergunta)).Returns(Task.CompletedTask);
        _formularioRepositoryMock.Setup(r => r.UpdateAsync(form)).Returns(Task.CompletedTask);

        var dto = new PerguntaDTO
        {
            Enunciado = "Qual seu comentário?",
            Tipo = TipoPergunta.TextoLongo,
            Ordem = 1
        };

        // Act
        var result = await _service.UpdatePerguntaAsync(perguntaId, dto);

        // Assert
        Assert.True(result.Status);
        Assert.Empty(pergunta.Alternativas);
        _alternativaRepositoryMock.Verify(r => r.DeleteAsync(alt1), Times.Once);
        _perguntaRepositoryMock.Verify(r => r.UpdateAsync(pergunta), Times.Once);
    }

    [Fact]
    public async Task UpdatePerguntasPorFormularioAsync_QuandoListaVazia_LancaArgumentException()
    {
        // Arrange
        int formId = 10;
        int userId = 5;
        var form = CreateFormulario(formId, criadorId: userId);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdatePerguntasPorFormularioAsync(formId, new List<PerguntaDTO>()));
        Assert.Equal("A lista de perguntas não pode ser vazia.", ex.Message);
    }

    [Fact]
    public async Task UpdatePerguntasPorFormularioAsync_QuandoIdsDuplicados_LancaArgumentException()
    {
        // Arrange
        int formId = 10;
        int userId = 5;
        var form = CreateFormulario(formId, criadorId: userId);

        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        var dtos = new List<PerguntaDTO>
        {
            new() { Id = 1, Enunciado = "P1", Tipo = TipoPergunta.RespostaCurta },
            new() { Id = 1, Enunciado = "P2", Tipo = TipoPergunta.RespostaCurta }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UpdatePerguntasPorFormularioAsync(formId, dtos));
        Assert.Equal("A lista contém IDs de perguntas duplicados.", ex.Message);
    }

    [Fact]
    public async Task DeletePerguntaAsync_QuandoNaoEncontrada_LancaNotFoundException()
    {
        // Arrange
        int perguntaId = 99;
        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync((PerguntaModel?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.DeletePerguntaAsync(perguntaId));
        Assert.Equal($"Pergunta com id {perguntaId} não encontrada.", ex.Message);
    }

    [Fact]
    public async Task DeletePerguntaAsync_QuandoValido_DeletaPerguntaEAtualizaFormulario()
    {
        // Arrange
        int perguntaId = 1;
        int formId = 10;
        int userId = 5;
        var pergunta = CreatePergunta(perguntaId, formId);
        var form = CreateFormulario(formId, criadorId: userId);

        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);
        _perguntaRepositoryMock.Setup(r => r.DeleteAsync(pergunta)).Returns(Task.CompletedTask);
        _formularioRepositoryMock.Setup(r => r.UpdateAsync(form)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeletePerguntaAsync(perguntaId);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Pergunta deletada com sucesso.", result.Message);
        _perguntaRepositoryMock.Verify(r => r.DeleteAsync(pergunta), Times.Once);
        _formularioRepositoryMock.Verify(r => r.UpdateAsync(form), Times.Once);
    }
}
