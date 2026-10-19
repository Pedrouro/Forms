using Forms.API.DTOs;
using Forms.API.Enums;
using Forms.API.Exceptions;
using Forms.API.Models;
using Forms.API.Repositories.Interfaces;
using Forms.API.Services.Implementations;
using Forms.API.Services.Interfaces;
using Moq;

namespace Forms.API.Tests.Services;

public class AlternativaServiceTests
{
    private readonly Mock<IAlternativaRepository> _alternativaRepositoryMock;
    private readonly Mock<IPerguntaRepository> _perguntaRepositoryMock;
    private readonly Mock<IFormularioRepository> _formularioRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly AlternativaService _service;

    public AlternativaServiceTests()
    {
        _alternativaRepositoryMock = new Mock<IAlternativaRepository>();
        _perguntaRepositoryMock = new Mock<IPerguntaRepository>();
        _formularioRepositoryMock = new Mock<IFormularioRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new AlternativaService(
            _alternativaRepositoryMock.Object,
            _perguntaRepositoryMock.Object,
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
            Titulo = "Formulário Teste",
            CriadorId = criadorId,
            Criador = criador,
            Status = status
        };
    }

    private PerguntaModel CreatePergunta(int id = 1, int formularioId = 1, TipoPergunta tipo = TipoPergunta.MultiplaEscolha) =>
        new()
        {
            Id = id,
            Enunciado = "Qual seu estado civil?",
            Tipo = tipo,
            Ordem = 1,
            FormularioId = formularioId
        };

    [Fact]
    public async Task AddAlternativaAsync_QuandoPerguntaNaoEncontrada_LancaNotFoundException()
    {
        // Arrange
        int perguntaId = 99;
        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync((PerguntaModel?)null);

        var dto = new AlternativaDTO { Texto = "Solteiro" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.AddAlternativaAsync(perguntaId, dto));
        Assert.Equal($"Pergunta com id {perguntaId} não encontrada.", ex.Message);
    }

    [Fact]
    public async Task AddAlternativaAsync_QuandoTipoPerguntaNaoPermitirAlternativas_LancaArgumentException()
    {
        // Arrange
        int perguntaId = 1;
        int formId = 10;
        int userId = 5;
        var pergunta = CreatePergunta(perguntaId, formId, TipoPergunta.RespostaCurta);
        var form = CreateFormulario(formId, criadorId: userId);

        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        var dto = new AlternativaDTO { Texto = "Opção Inválida" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAlternativaAsync(perguntaId, dto));
        Assert.Contains("não é possível adicionar alternativas", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddAlternativaAsync_QuandoTextoEstiverVazio_LancaArgumentException()
    {
        // Arrange
        int perguntaId = 1;
        int formId = 10;
        int userId = 5;
        var pergunta = CreatePergunta(perguntaId, formId, TipoPergunta.MultiplaEscolha);
        var form = CreateFormulario(formId, criadorId: userId);

        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        var dto = new AlternativaDTO { Texto = "   " };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAlternativaAsync(perguntaId, dto));
        Assert.Equal("O texto da alternativa é obrigatório.", ex.Message);
    }

    [Fact]
    public async Task AddAlternativaAsync_QuandoTextoDuplicadoNaPergunta_LancaArgumentException()
    {
        // Arrange
        int perguntaId = 1;
        int formId = 10;
        int userId = 5;
        var pergunta = CreatePergunta(perguntaId, formId, TipoPergunta.MultiplaEscolha);
        pergunta.Alternativas.Add(new AlternativaModel { Id = 1, Texto = "Solteiro", PerguntaId = perguntaId });

        var form = CreateFormulario(formId, criadorId: userId);

        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        var dto = new AlternativaDTO { Texto = "solteiro" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddAlternativaAsync(perguntaId, dto));
        Assert.Equal("Já existe uma alternativa com esse texto nesta pergunta.", ex.Message);
    }

    [Fact]
    public async Task AddAlternativaAsync_QuandoValido_AdicionaAlternativaEAtualizaFormulario()
    {
        // Arrange
        int perguntaId = 1;
        int formId = 10;
        int userId = 5;
        var pergunta = CreatePergunta(perguntaId, formId, TipoPergunta.MultiplaEscolha);
        var form = CreateFormulario(formId, criadorId: userId);

        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);
        _alternativaRepositoryMock.Setup(r => r.AddAsync(It.IsAny<AlternativaModel>())).ReturnsAsync((AlternativaModel a) => a);
        _formularioRepositoryMock.Setup(r => r.UpdateAsync(form)).Returns(Task.CompletedTask);

        var dto = new AlternativaDTO { Texto = "Casado" };

        // Act
        var result = await _service.AddAlternativaAsync(perguntaId, dto);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Alternativa adicionada com sucesso.", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("Casado", result.Data.Texto);
        _alternativaRepositoryMock.Verify(r => r.AddAsync(It.Is<AlternativaModel>(a => a.Texto == "Casado" && a.PerguntaId == perguntaId)), Times.Once);
        _formularioRepositoryMock.Verify(r => r.UpdateAsync(form), Times.Once);
    }

    [Fact]
    public async Task GetAlternativasByPerguntaIdAsync_QuandoPerguntaNaoEncontrada_LancaNotFoundException()
    {
        // Arrange
        int perguntaId = 99;
        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync((PerguntaModel?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAlternativasByPerguntaIdAsync(perguntaId));
        Assert.Equal($"Pergunta com id {perguntaId} não encontrada.", ex.Message);
    }

    [Fact]
    public async Task GetAlternativasByPerguntaIdAsync_QuandoValido_RetornaListaDeAlternativas()
    {
        // Arrange
        int perguntaId = 1;
        int formId = 10;
        int userId = 5;
        var pergunta = CreatePergunta(perguntaId, formId, TipoPergunta.MultiplaEscolha);
        pergunta.Alternativas.Add(new AlternativaModel { Id = 1, Texto = "Opção 1", PerguntaId = perguntaId });
        pergunta.Alternativas.Add(new AlternativaModel { Id = 2, Texto = "Opção 2", PerguntaId = perguntaId });

        var form = CreateFormulario(formId, criadorId: userId);

        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act
        var result = await _service.GetAlternativasByPerguntaIdAsync(perguntaId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAlternativaByIdAsync_QuandoAlternativaNaoEncontrada_LancaNotFoundException()
    {
        // Arrange
        int altId = 99;
        _alternativaRepositoryMock.Setup(r => r.GetByIdAsync(altId)).ReturnsAsync((AlternativaModel?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAlternativaByIdAsync(altId));
        Assert.Equal($"Alternativa com id {altId} não encontrada.", ex.Message);
    }

    [Fact]
    public async Task GetAlternativaByIdAsync_QuandoValido_RetornaAlternativa()
    {
        // Arrange
        int altId = 1;
        int perguntaId = 2;
        int formId = 3;
        int userId = 4;

        var pergunta = CreatePergunta(perguntaId, formId);
        var alternativa = new AlternativaModel { Id = altId, Texto = "Opção A", PerguntaId = perguntaId, Pergunta = pergunta };
        var form = CreateFormulario(formId, criadorId: userId);

        _alternativaRepositoryMock.Setup(r => r.GetByIdAsync(altId)).ReturnsAsync(alternativa);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        // Act
        var result = await _service.GetAlternativaByIdAsync(altId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(altId, result.Id);
        Assert.Equal("Opção A", result.Texto);
    }

    [Fact]
    public async Task UpdateAlternativaAsync_QuandoTextoDuplicado_LancaArgumentException()
    {
        // Arrange
        int altId = 1;
        int perguntaId = 2;
        int formId = 3;
        int userId = 4;

        var alternativa = new AlternativaModel { Id = altId, Texto = "Opção 1", PerguntaId = perguntaId };
        var pergunta = CreatePergunta(perguntaId, formId);
        pergunta.Alternativas.Add(alternativa);
        pergunta.Alternativas.Add(new AlternativaModel { Id = 2, Texto = "Opção 2", PerguntaId = perguntaId });

        var form = CreateFormulario(formId, criadorId: userId);

        _alternativaRepositoryMock.Setup(r => r.GetByIdAsync(altId)).ReturnsAsync(alternativa);
        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);

        var dto = new AlternativaDTO { Texto = "Opção 2" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAlternativaAsync(altId, dto));
        Assert.Equal("Já existe uma alternativa com esse texto nesta pergunta.", ex.Message);
    }

    [Fact]
    public async Task UpdateAlternativaAsync_QuandoValido_AtualizaComSucesso()
    {
        // Arrange
        int altId = 1;
        int perguntaId = 2;
        int formId = 3;
        int userId = 4;

        var alternativa = new AlternativaModel { Id = altId, Texto = "Opção Antiga", PerguntaId = perguntaId };
        var pergunta = CreatePergunta(perguntaId, formId);
        pergunta.Alternativas.Add(alternativa);

        var form = CreateFormulario(formId, criadorId: userId);

        _alternativaRepositoryMock.Setup(r => r.GetByIdAsync(altId)).ReturnsAsync(alternativa);
        _perguntaRepositoryMock.Setup(r => r.GetByIdAsync(perguntaId)).ReturnsAsync(pergunta);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);
        _alternativaRepositoryMock.Setup(r => r.UpdateAsync(alternativa)).Returns(Task.CompletedTask);
        _formularioRepositoryMock.Setup(r => r.UpdateAsync(form)).Returns(Task.CompletedTask);

        var dto = new AlternativaDTO { Texto = "Opção Nova" };

        // Act
        var result = await _service.UpdateAlternativaAsync(altId, dto);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Alternativa atualizada com sucesso.", result.Message);
        Assert.Equal("Opção Nova", alternativa.Texto);
        _alternativaRepositoryMock.Verify(r => r.UpdateAsync(alternativa), Times.Once);
        _formularioRepositoryMock.Verify(r => r.UpdateAsync(form), Times.Once);
    }

    [Fact]
    public async Task DeleteAlternativaAsync_QuandoNaoEncontrada_LancaNotFoundException()
    {
        // Arrange
        int altId = 99;
        _alternativaRepositoryMock.Setup(r => r.GetByIdAsync(altId)).ReturnsAsync((AlternativaModel?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAlternativaAsync(altId));
        Assert.Equal($"Alternativa com id {altId} não encontrada.", ex.Message);
    }

    [Fact]
    public async Task DeleteAlternativaAsync_QuandoValido_DeletaAlternativaEAtualizaFormulario()
    {
        // Arrange
        int altId = 1;
        int perguntaId = 2;
        int formId = 3;
        int userId = 4;

        var alternativa = new AlternativaModel { Id = altId, Texto = "Opção A", PerguntaId = perguntaId };
        var pergunta = CreatePergunta(perguntaId, formId);
        alternativa.Pergunta = pergunta;
        var form = CreateFormulario(formId, criadorId: userId);

        _alternativaRepositoryMock.Setup(r => r.GetByIdAsync(altId)).ReturnsAsync(alternativa);
        _formularioRepositoryMock.Setup(r => r.GetByIdAsync(formId)).ReturnsAsync(form);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserId()).Returns(userId);
        _currentUserServiceMock.Setup(c => c.GetCurrentUserPerfil()).Returns(PerfilUsuario.Comum);
        _alternativaRepositoryMock.Setup(r => r.DeleteAsync(alternativa)).Returns(Task.CompletedTask);
        _formularioRepositoryMock.Setup(r => r.UpdateAsync(form)).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAlternativaAsync(altId);

        // Assert
        Assert.True(result.Status);
        Assert.Equal("Alternativa deletada com sucesso.", result.Message);
        _alternativaRepositoryMock.Verify(r => r.DeleteAsync(alternativa), Times.Once);
        _formularioRepositoryMock.Verify(r => r.UpdateAsync(form), Times.Once);
    }
}
