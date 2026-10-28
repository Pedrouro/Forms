import { useEffect, useState } from 'react';
import type {
  AuthSession,
  FormularioResponseDTO,
  PerguntaDTO,
  StatusFormulario,
  TipoPergunta,
} from '../types/api';
import { formularioApi, getSavedSubmissions } from '../services/api';

interface FormsDashboardProps {
  session: AuthSession;
  onEditForm: (formId: number) => void;
  onRespondForm: (formId: number) => void;
  onNotify: (msg: string, type?: 'success' | 'error' | 'info') => void;
}

export function FormsDashboard({
  session,
  onEditForm,
  onRespondForm,
  onNotify,
}: FormsDashboardProps) {
  const [forms, setForms] = useState<FormularioResponseDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'Todos' | StatusFormulario>('Todos');
  const [lookupId, setLookupId] = useState('');

  // Create Modal state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [creating, setCreating] = useState(false);
  const [newTitulo, setNewTitulo] = useState('');
  const [newDescricao, setNewDescricao] = useState('');
  const [newStatus, setNewStatus] = useState<StatusFormulario>('Rascunho');
  const [initialQuestions, setInitialQuestions] = useState<PerguntaDTO[]>([]);

  const loadForms = async () => {
    setLoading(true);
    try {
      const list = await formularioApi.getAccessibleForms(session.perfil === 'Administrador');
      setForms(list);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao carregar formulários.';
      onNotify(msg, 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadForms();
  }, [session.perfil]);

  const handleLookupById = async (e: React.FormEvent) => {
    e.preventDefault();
    const id = Number(lookupId);
    if (!id || id <= 0) {
      onNotify('Informe um ID numérico válido.', 'error');
      return;
    }
    try {
      const found = await formularioApi.getById(id);
      setForms((prev) => {
        const filtered = prev.filter((f) => f.id !== found.id);
        return [found, ...filtered];
      });
      setLookupId('');
      onNotify(`Formulário #${found.id} ("${found.titulo}") carregado!`, 'success');
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Formulário não encontrado.';
      onNotify(msg, 'error');
    }
  };

  const handleAddInitialQuestion = () => {
    setInitialQuestions((prev) => [
      ...prev,
      {
        enunciado: '',
        tipo: 'RespostaCurta',
        ordem: prev.length + 1,
        obrigatoria: false,
        alternativas: [],
      },
    ]);
  };

  const handleUpdateInitialQuestion = (index: number, patch: Partial<PerguntaDTO>) => {
    setInitialQuestions((prev) =>
      prev.map((q, i) => {
        if (i !== index) return q;
        const updated = { ...q, ...patch };
        if (
          patch.tipo &&
          patch.tipo !== 'MultiplaEscolha' &&
          patch.tipo !== 'CaixaDeSelecao'
        ) {
          updated.alternativas = [];
        } else if (
          patch.tipo &&
          (patch.tipo === 'MultiplaEscolha' || patch.tipo === 'CaixaDeSelecao') &&
          (!updated.alternativas || updated.alternativas.length === 0)
        ) {
          updated.alternativas = [{ texto: 'Opção 1' }, { texto: 'Opção 2' }];
        }
        return updated;
      })
    );
  };

  const handleRemoveInitialQuestion = (index: number) => {
    setInitialQuestions((prev) =>
      prev.filter((_, i) => i !== index).map((q, idx) => ({ ...q, ordem: idx + 1 }))
    );
  };

  const handleCreateForm = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTitulo.trim()) {
      onNotify('O título do formulário é obrigatório.', 'error');
      return;
    }

    setCreating(true);
    try {
      const cleanedQuestions: PerguntaDTO[] = initialQuestions
        .filter((q) => q.enunciado.trim() !== '')
        .map((q, idx) => ({
          enunciado: q.enunciado.trim(),
          tipo: q.tipo,
          ordem: idx + 1,
          obrigatoria: q.obrigatoria,
          alternativas:
            q.tipo === 'MultiplaEscolha' || q.tipo === 'CaixaDeSelecao'
              ? (q.alternativas || [])
                  .filter((a) => a.texto.trim() !== '')
                  .map((a) => ({ texto: a.texto.trim() }))
              : undefined,
        }));

      const res = await formularioApi.create({
        titulo: newTitulo.trim(),
        descricao: newDescricao.trim() || undefined,
        status: newStatus,
        perguntas: cleanedQuestions.length > 0 ? cleanedQuestions : undefined,
      });

      onNotify(res.message || 'Formulário criado com sucesso!', 'success');
      setShowCreateModal(false);
      setNewTitulo('');
      setNewDescricao('');
      setNewStatus('Rascunho');
      setInitialQuestions([]);

      if (res.data) {
        onEditForm(res.data.id);
      } else {
        await loadForms();
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao criar formulário.';
      onNotify(msg, 'error');
    } finally {
      setCreating(false);
    }
  };

  const handleQuickStatusChange = async (
    form: FormularioResponseDTO,
    nextStatus: StatusFormulario
  ) => {
    try {
      const res = await formularioApi.update(form.id, {
        titulo: form.titulo,
        descricao: form.descricao,
        status: nextStatus,
      });
      onNotify(res.message || `Status alterado para ${nextStatus}.`, 'success');
      setForms((prev) =>
        prev.map((f) => (f.id === form.id ? { ...f, status: nextStatus } : f))
      );
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao atualizar status.';
      onNotify(msg, 'error');
    }
  };

  const handleDeleteForm = async (form: FormularioResponseDTO) => {
    if (!window.confirm(`Deseja realmente excluir o formulário "${form.titulo}"?`)) {
      return;
    }
    try {
      const res = await formularioApi.remove(form.id);
      onNotify(res.message || 'Formulário excluído com sucesso.', 'success');
      setForms((prev) => prev.filter((f) => f.id !== form.id));
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao excluir formulário.';
      onNotify(msg, 'error');
    }
  };

  const handleShareLink = (formId: number) => {
    const shareUrl = `${window.location.origin}${window.location.pathname}?responder=${formId}`;
    navigator.clipboard
      .writeText(shareUrl)
      .then(() => {
        onNotify(`Link de compartilhamento copiado: ${shareUrl}`, 'info');
      })
      .catch(() => {
        onNotify(`Link do formulário: ${shareUrl}`, 'info');
      });
  };

  const filteredForms = forms.filter((f) => {
    const matchesStatus = statusFilter === 'Todos' || f.status === statusFilter;
    const query = search.toLowerCase().trim();
    const matchesSearch =
      !query ||
      f.titulo.toLowerCase().includes(query) ||
      (f.descricao && f.descricao.toLowerCase().includes(query)) ||
      String(f.id) === query;
    return matchesStatus && matchesSearch;
  });

  const totalCount = forms.length;
  const publicadosCount = forms.filter((f) => f.status === 'Publicado').length;
  const rascunhosCount = forms.filter((f) => f.status === 'Rascunho').length;
  const encerradosCount = forms.filter((f) => f.status === 'Encerrado').length;

  return (
    <div className="page-container">
      <div className="page-header">
        <div>
          <h1 className="page-title">Meus Formulários</h1>
          <p className="page-subtitle">
            Crie, edite perguntas e alternativas, publique e compartilhe seus formulários.
          </p>
        </div>
        <div className="header-actions">
          <button
            type="button"
            className="btn btn-outline"
            onClick={() => void loadForms()}
            disabled={loading}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="23 4 23 10 17 10" />
              <polyline points="1 20 1 14 7 14" />
              <path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15" />
            </svg>
            Atualizar
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => setShowCreateModal(true)}
          >
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            Novo Formulário
          </button>
        </div>
      </div>

      {/* Summary Statistics */}
      <div className="stats-grid">
        <div className="stat-card">
          <span className="stat-label">Total Carregados</span>
          <strong className="stat-value">{totalCount}</strong>
        </div>
        <div className="stat-card stat-blue">
          <span className="stat-label">Publicados (Ativos)</span>
          <strong className="stat-value">{publicadosCount}</strong>
        </div>
        <div className="stat-card stat-light">
          <span className="stat-label">Em Rascunho</span>
          <strong className="stat-value">{rascunhosCount}</strong>
        </div>
        <div className="stat-card stat-muted">
          <span className="stat-label">Encerrados</span>
          <strong className="stat-value">{encerradosCount}</strong>
        </div>
      </div>

      {/* Filter & Lookup Toolbar */}
      <div className="toolbar-card">
        <div className="toolbar-search">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="8" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            type="text"
            placeholder="Filtrar por título, descrição ou ID..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="toolbar-pills">
          {(['Todos', 'Publicado', 'Rascunho', 'Encerrado'] as const).map((status) => (
            <button
              key={status}
              type="button"
              className={`filter-pill ${statusFilter === status ? 'active' : ''}`}
              onClick={() => setStatusFilter(status)}
            >
              {status}
            </button>
          ))}
        </div>

        <form className="toolbar-lookup" onSubmit={handleLookupById}>
          <input
            type="number"
            min="1"
            placeholder="Buscar ID na API..."
            value={lookupId}
            onChange={(e) => setLookupId(e.target.value)}
          />
          <button type="submit" className="btn btn-outline btn-sm">
            Abrir ID
          </button>
        </form>
      </div>

      {/* Forms Grid */}
      {loading ? (
        <div className="empty-state">
          <div className="spinner" />
          <p>Carregando formulários da API...</p>
        </div>
      ) : filteredForms.length === 0 ? (
        <div className="empty-state">
          <div className="empty-icon">
            <svg width="36" height="36" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
              <line x1="12" y1="18" x2="12" y2="12" />
              <line x1="9" y1="15" x2="15" y2="15" />
            </svg>
          </div>
          <h3>Nenhum formulário encontrado</h3>
          <p>
            Crie seu primeiro formulário personalizado ou busque um formulário existente pelo ID.
          </p>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => setShowCreateModal(true)}
          >
            Criar Primeiro Formulário
          </button>
        </div>
      ) : (
        <div className="forms-grid">
          {filteredForms.map((form) => {
            const submissionsCount = getSavedSubmissions(form.id).length;
            return (
              <div key={form.id} className="form-card">
                <div className="form-card-top">
                  <div className="form-card-badges">
                    <span className="id-badge">#{form.id}</span>
                    <span className={`status-badge status-${form.status.toLowerCase()}`}>
                      {form.status}
                    </span>
                  </div>
                  <div className="form-card-quick-actions">
                    <button
                      type="button"
                      className="btn-icon-subtle"
                      title="Copiar link de compartilhamento"
                      onClick={() => handleShareLink(form.id)}
                    >
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <circle cx="18" cy="5" r="3" />
                        <circle cx="6" cy="12" r="3" />
                        <circle cx="18" cy="19" r="3" />
                        <line x1="8.59" y1="13.51" x2="15.42" y2="17.49" />
                        <line x1="15.41" y1="6.51" x2="8.59" y2="10.49" />
                      </svg>
                    </button>
                    <button
                      type="button"
                      className="btn-icon-subtle danger"
                      title="Excluir formulário"
                      onClick={() => void handleDeleteForm(form)}
                    >
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <polyline points="3 6 5 6 21 6" />
                        <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                      </svg>
                    </button>
                  </div>
                </div>

                <h3 className="form-card-title">{form.titulo}</h3>
                <p className="form-card-desc">
                  {form.descricao || 'Sem descrição adicional.'}
                </p>

                <div className="form-card-meta">
                  <span>
                    <strong>{form.perguntas?.length || 0}</strong> pergunta(s)
                  </span>
                  <span>•</span>
                  <span>
                    <strong>{submissionsCount}</strong> resposta(s)
                  </span>
                  <span>•</span>
                  <span>
                    Criado em {new Date(form.dataCriacao).toLocaleDateString('pt-BR')}
                  </span>
                </div>

                <div className="form-card-status-bar">
                  {form.status !== 'Publicado' && (
                    <button
                      type="button"
                      className="btn-tiny btn-tiny-publish"
                      onClick={() => void handleQuickStatusChange(form, 'Publicado')}
                    >
                      Publicar
                    </button>
                  )}
                  {form.status === 'Publicado' && (
                    <button
                      type="button"
                      className="btn-tiny btn-tiny-close"
                      onClick={() => void handleQuickStatusChange(form, 'Encerrado')}
                    >
                      Encerrar Respostas
                    </button>
                  )}
                </div>

                <div className="form-card-footer">
                  <button
                    type="button"
                    className="btn btn-primary btn-sm"
                    onClick={() => onEditForm(form.id)}
                  >
                    Editar Perguntas
                  </button>
                  <button
                    type="button"
                    className="btn btn-outline btn-sm"
                    onClick={() => onRespondForm(form.id)}
                  >
                    Preencher / Respostas
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Create Form Modal */}
      {showCreateModal && (
        <div className="modal-backdrop" onClick={() => setShowCreateModal(false)}>
          <div className="modal-panel" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Criar Novo Formulário</h2>
              <button
                type="button"
                className="btn-icon-subtle"
                onClick={() => setShowCreateModal(false)}
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleCreateForm} className="modal-body">
              <div className="form-group">
                <label htmlFor="new-form-titulo">Título do Formulário *</label>
                <input
                  id="new-form-titulo"
                  type="text"
                  required
                  maxLength={200}
                  placeholder="Ex: Pesquisa de Satisfação 2026"
                  value={newTitulo}
                  onChange={(e) => setNewTitulo(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label htmlFor="new-form-desc">Descrição</label>
                <textarea
                  id="new-form-desc"
                  rows={3}
                  maxLength={1000}
                  placeholder="Descreva o objetivo deste formulário..."
                  value={newDescricao}
                  onChange={(e) => setNewDescricao(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label htmlFor="new-form-status">Status Inicial</label>
                <select
                  id="new-form-status"
                  value={newStatus}
                  onChange={(e) => setNewStatus(e.target.value as StatusFormulario)}
                >
                  <option value="Rascunho">Rascunho (Em edição privada)</option>
                  <option value="Publicado">Publicado (Aberto para respostas)</option>
                  <option value="Encerrado">Encerrado</option>
                </select>
              </div>

              <div className="initial-questions-section">
                <div className="section-row-between">
                  <h4>Perguntas Iniciais (Opcional)</h4>
                  <button
                    type="button"
                    className="btn btn-outline btn-sm"
                    onClick={handleAddInitialQuestion}
                  >
                    + Adicionar Pergunta
                  </button>
                </div>

                {initialQuestions.map((q, qIdx) => (
                  <div key={qIdx} className="nested-question-box">
                    <div className="nested-question-header">
                      <strong>Pergunta #{qIdx + 1}</strong>
                      <button
                        type="button"
                        className="btn-link danger"
                        onClick={() => handleRemoveInitialQuestion(qIdx)}
                      >
                        Remover
                      </button>
                    </div>

                    <div className="form-row-2">
                      <input
                        type="text"
                        placeholder="Enunciado da pergunta..."
                        value={q.enunciado}
                        onChange={(e) =>
                          handleUpdateInitialQuestion(qIdx, { enunciado: e.target.value })
                        }
                      />
                      <select
                        value={q.tipo}
                        onChange={(e) =>
                          handleUpdateInitialQuestion(qIdx, {
                            tipo: e.target.value as TipoPergunta,
                          })
                        }
                      >
                        <option value="RespostaCurta">Resposta Curta</option>
                        <option value="TextoLongo">Texto Longo</option>
                        <option value="MultiplaEscolha">Múltipla Escolha</option>
                        <option value="CaixaDeSelecao">Caixa de Seleção</option>
                      </select>
                    </div>

                    <label className="checkbox-inline">
                      <input
                        type="checkbox"
                        checked={q.obrigatoria}
                        onChange={(e) =>
                          handleUpdateInitialQuestion(qIdx, {
                            obrigatoria: e.target.checked,
                          })
                        }
                      />
                      <span>Pergunta obrigatória</span>
                    </label>

                    {(q.tipo === 'MultiplaEscolha' || q.tipo === 'CaixaDeSelecao') && (
                      <div className="nested-alternatives">
                        <span className="small-label">Alternativas:</span>
                        {(q.alternativas || []).map((alt, aIdx) => (
                          <div key={aIdx} className="alt-inline-row">
                            <input
                              type="text"
                              placeholder={`Alternativa ${aIdx + 1}`}
                              value={alt.texto}
                              onChange={(e) => {
                                const list = [...(q.alternativas || [])];
                                list[aIdx] = { ...list[aIdx], texto: e.target.value };
                                handleUpdateInitialQuestion(qIdx, { alternativas: list });
                              }}
                            />
                            <button
                              type="button"
                              className="btn-icon-subtle danger"
                              onClick={() => {
                                const list = (q.alternativas || []).filter(
                                  (_, idx) => idx !== aIdx
                                );
                                handleUpdateInitialQuestion(qIdx, { alternativas: list });
                              }}
                            >
                              ✕
                            </button>
                          </div>
                        ))}
                        <button
                          type="button"
                          className="btn-link"
                          onClick={() => {
                            const list = [
                              ...(q.alternativas || []),
                              { texto: `Opção ${(q.alternativas?.length || 0) + 1}` },
                            ];
                            handleUpdateInitialQuestion(qIdx, { alternativas: list });
                          }}
                        >
                          + Adicionar alternativa
                        </button>
                      </div>
                    )}
                  </div>
                ))}
              </div>

              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={() => setShowCreateModal(false)}
                >
                  Cancelar
                </button>
                <button type="submit" className="btn btn-primary" disabled={creating}>
                  {creating ? 'Criando...' : 'Criar Formulário'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
