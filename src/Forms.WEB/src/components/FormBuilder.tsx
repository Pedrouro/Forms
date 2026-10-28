import { useEffect, useState } from 'react';
import type {
  AlternativaResponseDTO,
  FormularioResponseDTO,
  PerguntaResponseDTO,
  StatusFormulario,
  TipoPergunta,
} from '../types/api';
import { alternativaApi, formularioApi, perguntaApi } from '../services/api';

interface FormBuilderProps {
  formId: number;
  onBack: () => void;
  onPreview: (formId: number) => void;
  onNotify: (msg: string, type?: 'success' | 'error' | 'info') => void;
}

const TIPO_PERGUNTA_LABELS: Record<TipoPergunta, string> = {
  RespostaCurta: 'Resposta Curta',
  TextoLongo: 'Texto Longo',
  MultiplaEscolha: 'Múltipla Escolha',
  CaixaDeSelecao: 'Caixa de Seleção',
};

export function FormBuilder({
  formId,
  onBack,
  onPreview,
  onNotify,
}: FormBuilderProps) {
  const [form, setForm] = useState<FormularioResponseDTO | null>(null);
  const [perguntas, setPerguntas] = useState<PerguntaResponseDTO[]>([]);
  const [loading, setLoading] = useState(true);
  const [savingForm, setSavingForm] = useState(false);

  // Form Header editable states
  const [titulo, setTitulo] = useState('');
  const [descricao, setDescricao] = useState('');
  const [status, setStatus] = useState<StatusFormulario>('Rascunho');

  // New Question card state
  const [newEnunciado, setNewEnunciado] = useState('');
  const [newTipo, setNewTipo] = useState<TipoPergunta>('RespostaCurta');
  const [newObrigatoria, setNewObrigatoria] = useState(false);
  const [newAlternativas, setNewAlternativas] = useState<string[]>(['Opção 1', 'Opção 2']);
  const [addingQuestion, setAddingQuestion] = useState(false);

  // Per-question new alternative input
  const [altInputByQuestion, setAltInputByQuestion] = useState<Record<number, string>>({});

  const loadFullForm = async () => {
    setLoading(true);
    try {
      const formData = await formularioApi.getById(formId);
      setForm(formData);
      setTitulo(formData.titulo);
      setDescricao(formData.descricao || '');
      setStatus(formData.status);

      try {
        const questionsData = await perguntaApi.getByFormulario(formId);
        setPerguntas(questionsData.sort((a, b) => a.ordem - b.ordem));
      } catch {
        setPerguntas((formData.perguntas || []).sort((a, b) => a.ordem - b.ordem));
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao carregar formulário.';
      onNotify(msg, 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadFullForm();
  }, [formId]);

  const isClosed = status === 'Encerrado';

  const handleSaveFormHeader = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!titulo.trim()) {
      onNotify('O título do formulário não pode ficar vazio.', 'error');
      return;
    }
    setSavingForm(true);
    try {
      const res = await formularioApi.update(formId, {
        titulo: titulo.trim(),
        descricao: descricao.trim() || undefined,
        status,
      });
      onNotify(res.message || 'Dados do formulário atualizados!', 'success');
      await loadFullForm();
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao atualizar formulário.';
      onNotify(msg, 'error');
    } finally {
      setSavingForm(false);
    }
  };

  // const handleQuestionTypeChanged = {


  // }

  const handleAddQuestion = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newEnunciado.trim()) {
      onNotify('Informe o enunciado da nova pergunta.', 'error');
      return;
    }

    const permiteAlt = newTipo === 'MultiplaEscolha' || newTipo === 'CaixaDeSelecao';
    const validAlts = permiteAlt
      ? newAlternativas.map((t) => t.trim()).filter(Boolean)
      : [];

    setAddingQuestion(true);
    try {
      const res = await perguntaApi.create({
        formularioId: formId,
        enunciado: newEnunciado.trim(),
        tipo: newTipo,
        ordem: perguntas.length + 1,
        obrigatoria: newObrigatoria,
        alternativas: validAlts.length > 0 ? validAlts.map((texto) => ({ texto })) : undefined,
      });

      onNotify(res.message || 'Pergunta adicionada com sucesso!', 'success');
      setNewEnunciado('');
      setNewObrigatoria(false);
      setNewAlternativas(['Opção 1', 'Opção 2']);
      if (res.data) {
        setPerguntas((prev) => [...prev, res.data!].sort((a, b) => a.ordem - b.ordem));
      } else {
        await loadFullForm();
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao criar pergunta.';
      onNotify(msg, 'error');
    } finally {
      setAddingQuestion(false);
    }
  };

  const handleQuestionTypeChange = async (
    q: PerguntaResponseDTO,
    novoTipo: TipoPergunta
  ) => {
    if (q.tipo === novoTipo) return;

    const permiteAlt =
      novoTipo === 'MultiplaEscolha' || novoTipo === 'CaixaDeSelecao';

    setPerguntas((prev) =>
      prev.map((item) =>
        item.id === q.id
          ? {
              ...item,
              tipo: novoTipo,
              alternativas: permiteAlt ? item.alternativas : [],
            }
          : item
      )
    );

    if (!permiteAlt) {
      setAltInputByQuestion((prev) => ({ ...prev, [q.id]: '' }));
    }

    try {
      const res = await perguntaApi.update(q.id, {
        id: q.id,
        formularioId: formId,
        enunciado: q.enunciado.trim(),
        tipo: novoTipo,
        ordem: q.ordem,
        obrigatoria: q.obrigatoria,
      });

      if (res.data) {
        setPerguntas((prev) =>
          prev.map((item) =>
            item.id === q.id
              ? {
                  ...res.data!,
                  alternativas: permiteAlt ? item.alternativas : [],
                }
              : item
          )
        );
      }

      onNotify(
        !permiteAlt && q.alternativas && q.alternativas.length > 0
          ? 'Tipo alterado para texto e alternativas excluídas!'
          : 'Tipo da pergunta atualizado!',
        'success'
      );
    } catch (err) {
      const msg =
        err instanceof Error ? err.message : 'Erro ao atualizar tipo da pergunta.';
      onNotify(msg, 'error');
      await loadFullForm();
    }
  };

  const handleSaveQuestion = async (q: PerguntaResponseDTO) => {
    try {
      const permiteAlt =
        q.tipo === 'MultiplaEscolha' || q.tipo === 'CaixaDeSelecao';

      const res = await perguntaApi.update(q.id, {
        id: q.id,
        formularioId: formId,
        enunciado: q.enunciado.trim(),
        tipo: q.tipo,
        ordem: q.ordem,
        obrigatoria: q.obrigatoria,
      });

      let savedAlternativas: AlternativaResponseDTO[] = res.data?.alternativas || [];

      if (permiteAlt && q.alternativas && q.alternativas.length > 0) {
        const altRes = await alternativaApi.updateByPergunta(
          q.id,
          q.alternativas.map((a) => ({
            id: a.id,
            perguntaId: q.id,
            texto: a.texto.trim(),
          }))
        );
        if (altRes.data) {
          savedAlternativas = altRes.data;
        }
      }

      onNotify(res.message || 'Pergunta salva!', 'success');
      if (res.data) {
        const updatedQuestion: PerguntaResponseDTO = {
          ...res.data,
          alternativas: permiteAlt ? savedAlternativas : [],
        };
        setPerguntas((prev) =>
          prev.map((item) => (item.id === q.id ? updatedQuestion : item))
        );
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao atualizar pergunta.';
      onNotify(msg, 'error');
    }
  };

  const handleBatchSaveQuestionsOrder = async (updatedList: PerguntaResponseDTO[]) => {
    try {
      const payload = updatedList.map((q, idx) => ({
        id: q.id,
        formularioId: formId,
        enunciado: q.enunciado.trim(),
        tipo: q.tipo,
        ordem: idx + 1,
        obrigatoria: q.obrigatoria,
      }));
      const res = await perguntaApi.updateByFormulario(formId, payload);

      const altsMap: Record<number, AlternativaResponseDTO[]> = {};
      for (const q of updatedList) {
        const permiteAlt =
          q.tipo === 'MultiplaEscolha' || q.tipo === 'CaixaDeSelecao';
        if (permiteAlt && q.alternativas && q.alternativas.length > 0) {
          const altRes = await alternativaApi.updateByPergunta(
            q.id,
            q.alternativas.map((a) => ({
              id: a.id,
              perguntaId: q.id,
              texto: a.texto.trim(),
            }))
          );
          if (altRes.data) {
            altsMap[q.id] = altRes.data;
          }
        }
      }

      onNotify(res.message || 'Perguntas atualizadas!', 'success');
      if (res.data) {
        setPerguntas(
          res.data.map((q) => ({
            ...q,
            alternativas: altsMap[q.id] ?? q.alternativas,
          }))
        );
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao salvar perguntas.';
      onNotify(msg, 'error');
    }
  };

  const handleMoveQuestion = (index: number, direction: -1 | 1) => {
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= perguntas.length) return;
    const copy = [...perguntas];
    const [moved] = copy.splice(index, 1);
    copy.splice(targetIndex, 0, moved);
    const reordered = copy.map((q, idx) => ({ ...q, ordem: idx + 1 }));
    setPerguntas(reordered);
    void handleBatchSaveQuestionsOrder(reordered);
  };

  const handleDeleteQuestion = async (perguntaId: number) => {
    if (!window.confirm('Deseja realmente remover esta pergunta?')) return;
    try {
      const res = await perguntaApi.remove(perguntaId);
      onNotify(res.message || 'Pergunta removida.', 'success');
      setPerguntas((prev) => prev.filter((q) => q.id !== perguntaId));
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao excluir pergunta.';
      onNotify(msg, 'error');
    }
  };

  // ==================== ALTERNATIVAS HANDLERS ====================
  const handleAddAlternative = async (perguntaId: number) => {
    const texto = (altInputByQuestion[perguntaId] || '').trim();
    if (!texto) {
      onNotify('Digite o texto da alternativa.', 'error');
      return;
    }
    try {
      const res = await alternativaApi.create({
        perguntaId,
        texto,
      });
      onNotify(res.message || 'Alternativa adicionada!', 'success');
      setAltInputByQuestion((prev) => ({ ...prev, [perguntaId]: '' }));
      if (res.data) {
        setPerguntas((prev) =>
          prev.map((q) =>
            q.id === perguntaId
              ? { ...q, alternativas: [...(q.alternativas || []), res.data!] }
              : q
          )
        );
      } else {
        const refreshedAlts = await alternativaApi.getByPergunta(perguntaId);
        setPerguntas((prev) =>
          prev.map((q) => (q.id === perguntaId ? { ...q, alternativas: refreshedAlts } : q))
        );
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao adicionar alternativa.';
      onNotify(msg, 'error');
    }
  };

  const handleDeleteAlternative = async (perguntaId: number, altId: number) => {
    try {
      const res = await alternativaApi.remove(altId);
      onNotify(res.message || 'Alternativa removida!', 'success');
      setPerguntas((prev) =>
        prev.map((q) =>
          q.id === perguntaId
            ? { ...q, alternativas: q.alternativas.filter((a) => a.id !== altId) }
            : q
        )
      );
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao remover alternativa.';
      onNotify(msg, 'error');
    }
  };

  if (loading) {
    return (
      <div className="page-container">
        <div className="empty-state">
          <div className="spinner" />
          <p>Carregando editor do formulário #{formId}...</p>
        </div>
      </div>
    );
  }

  if (!form) {
    return (
      <div className="page-container">
        <div className="empty-state">
          <h3>Formulário não encontrado</h3>
          <button type="button" className="btn btn-outline" onClick={onBack}>
            Voltar para Formulários
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <div className="builder-topbar">
        <button type="button" className="btn btn-outline btn-sm" onClick={onBack}>
          ← Voltar aos Formulários
        </button>
        <div className="builder-topbar-actions">
          <button
            type="button"
            className="btn btn-outline btn-sm"
            onClick={() => onPreview(formId)}
          >
            Visualizar / Preencher
          </button>
        </div>
      </div>

      {/* Form Header Metadata Card */}
      <form className="builder-header-card" onSubmit={handleSaveFormHeader}>
        <div className="builder-header-strip" />
        <div className="builder-header-content">
          <div className="section-row-between">
            <span className="id-badge">Formulário #{form.id}</span>
            <span className={`status-badge status-${status.toLowerCase()}`}>{status}</span>
          </div>

          <div className="form-group">
            <label htmlFor="builder-titulo">Título do Formulário</label>
            <input
              id="builder-titulo"
              type="text"
              required
              value={titulo}
              onChange={(e) => setTitulo(e.target.value)}
            />
          </div>

          <div className="form-row-2">
            <div className="form-group">
              <label htmlFor="builder-desc">Descrição</label>
              <input
                id="builder-desc"
                type="text"
                placeholder="Adicione uma descrição para os respondentes..."
                value={descricao}
                onChange={(e) => setDescricao(e.target.value)}
              />
            </div>

            <div className="form-group">
              <label htmlFor="builder-status">Status do Formulário</label>
              <select
                id="builder-status"
                value={status}
                onChange={(e) => setStatus(e.target.value as StatusFormulario)}
              >
                <option value="Rascunho">Rascunho</option>
                <option value="Publicado">Publicado</option>
                <option value="Encerrado">Encerrado</option>
              </select>
            </div>
          </div>

          <div className="builder-header-footer">
            <span className="meta-hint">
              {form.dataAtualizacao
                ? `Última atualização: ${new Date(form.dataAtualizacao).toLocaleString('pt-BR')}`
                : `Criado em: ${new Date(form.dataCriacao).toLocaleString('pt-BR')}`}
            </span>
            <button type="submit" className="btn btn-primary" disabled={savingForm}>
              {savingForm ? 'Salvando...' : 'Salvar Cabeçalho'}
            </button>
          </div>
        </div>
      </form>

      {isClosed && (
        <div className="alert alert-info">
          Este formulário está com status <strong>Encerrado</strong>. A API bloqueia alterações
          em perguntas e alternativas enquanto o formulário estiver encerrado. Para editar as
          perguntas, altere o status acima para <strong>Rascunho</strong> ou{' '}
          <strong>Publicado</strong> e clique em &ldquo;Salvar Cabeçalho&rdquo;.
        </div>
      )}

      {/* Existing Questions List */}
      <div className="questions-section">
        <div className="section-row-between">
          <h2 className="section-title">Perguntas ({perguntas.length})</h2>
          {perguntas.length > 1 && !isClosed && (
            <button
              type="button"
              className="btn btn-outline btn-sm"
              onClick={() => void handleBatchSaveQuestionsOrder(perguntas)}
            >
              Salvar Todas as Perguntas em Lote
            </button>
          )}
        </div>

        {perguntas.length === 0 ? (
          <div className="empty-card">
            <p>Nenhuma pergunta cadastrada ainda. Utilize o painel abaixo para adicionar perguntas.</p>
          </div>
        ) : (
          <div className="questions-list">
            {perguntas.map((q, index) => {
              const permiteAlt =
                q.tipo === 'MultiplaEscolha' || q.tipo === 'CaixaDeSelecao';

              return (
                <div key={q.id} className="question-card">
                  <div className="question-card-header">
                    <div className="question-order-controls">
                      <span className="question-number">#{index + 1}</span>
                      <button
                        type="button"
                        className="btn-icon-subtle"
                        disabled={index === 0 || isClosed}
                        title="Mover para cima"
                        onClick={() => handleMoveQuestion(index, -1)}
                      >
                        ↑
                      </button>
                      <button
                        type="button"
                        className="btn-icon-subtle"
                        disabled={index === perguntas.length - 1 || isClosed}
                        title="Mover para baixo"
                        onClick={() => handleMoveQuestion(index, 1)}
                      >
                        ↓
                      </button>
                      <span className="question-type-pill">
                        {TIPO_PERGUNTA_LABELS[q.tipo]}
                      </span>
                    </div>

                    <div className="question-actions">
                      <button
                        type="button"
                        className="btn btn-outline btn-sm"
                        disabled={isClosed}
                        onClick={() => void handleSaveQuestion(q)}
                      >
                        Salvar Pergunta
                      </button>
                      <button
                        type="button"
                        className="btn-icon-subtle danger"
                        disabled={isClosed}
                        title="Excluir pergunta"
                        onClick={() => void handleDeleteQuestion(q.id)}
                      >
                        Excluir
                      </button>
                    </div>
                  </div>

                  <div className="form-row-2">
                    <div className="form-group">
                      <label>Enunciado da Pergunta</label>
                      <input
                        type="text"
                        disabled={isClosed}
                        value={q.enunciado}
                        onChange={(e) =>
                          setPerguntas((prev) =>
                            prev.map((item) =>
                              item.id === q.id ? { ...item, enunciado: e.target.value } : item
                            )
                          )
                        }
                      />
                    </div>

                    <div className="form-group">
                      <label>Tipo de Resposta</label>
                      <select
                        disabled={isClosed}
                        value={q.tipo}
                        onChange={(e) =>
                          void handleQuestionTypeChange(q, e.target.value as TipoPergunta)
                        }
                      >
                        <option value="RespostaCurta">Resposta Curta</option>
                        <option value="TextoLongo">Texto Longo</option>
                        <option value="MultiplaEscolha">Múltipla Escolha</option>
                        <option value="CaixaDeSelecao">Caixa de Seleção</option>
                      </select>
                    </div>
                  </div>

                  <label className="checkbox-inline">
                    <input
                      type="checkbox"
                      disabled={isClosed}
                      checked={q.obrigatoria}
                      onChange={(e) =>
                        setPerguntas((prev) =>
                          prev.map((item) =>
                            item.id === q.id
                              ? { ...item, obrigatoria: e.target.checked }
                              : item
                          )
                        )
                      }
                    />
                    <span>Resposta obrigatória</span>
                  </label>

                  {/* Alternativas Manager */}
                  {permiteAlt && (
                    <div className="alternatives-panel">
                      <div className="section-row-between">
                        <span className="small-label">
                          Alternativas ({q.alternativas?.length || 0})
                        </span>
                      </div>

                      <div className="alternatives-list">
                        {(q.alternativas || []).map((alt) => (
                          <div key={alt.id} className="alt-item-row">
                            <span className="alt-bullet">
                              {q.tipo === 'MultiplaEscolha' ? '○' : '☐'}
                            </span>
                            <input
                              type="text"
                              disabled={isClosed}
                              value={alt.texto}
                              onChange={(e) =>
                                setPerguntas((prev) =>
                                  prev.map((item) =>
                                    item.id === q.id
                                      ? {
                                          ...item,
                                          alternativas: item.alternativas.map((a) =>
                                            a.id === alt.id
                                              ? { ...a, texto: e.target.value }
                                              : a
                                          ),
                                        }
                                      : item
                                  )
                                )
                              }
                            />
                            <button
                              type="button"
                              className="btn-icon-subtle danger"
                              disabled={isClosed}
                              title="Remover alternativa"
                              onClick={() => void handleDeleteAlternative(q.id, alt.id)}
                            >
                              ✕
                            </button>
                          </div>
                        ))}
                      </div>

                      {!isClosed && (
                        <div className="add-alt-bar">
                          <input
                            type="text"
                            placeholder="Nova alternativa..."
                            value={altInputByQuestion[q.id] || ''}
                            onChange={(e) =>
                              setAltInputByQuestion((prev) => ({
                                ...prev,
                                [q.id]: e.target.value,
                              }))
                            }
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') {
                                e.preventDefault();
                                void handleAddAlternative(q.id);
                              }
                            }}
                          />
                          <button
                            type="button"
                            className="btn btn-outline btn-sm"
                            onClick={() => void handleAddAlternative(q.id)}
                          >
                            + Adicionar Alternativa
                          </button>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Add New Question Panel */}
      {!isClosed && (
        <form className="add-question-card" onSubmit={handleAddQuestion}>
          <h3>Adicionar Nova Pergunta</h3>
          <div className="form-row-2">
            <div className="form-group">
              <label htmlFor="new-q-enunciado">Enunciado da Pergunta *</label>
              <input
                id="new-q-enunciado"
                type="text"
                required
                placeholder="Ex: Qual seu nível de satisfação com o atendimento?"
                value={newEnunciado}
                onChange={(e) => setNewEnunciado(e.target.value)}
              />
            </div>

            <div className="form-group">
              <label htmlFor="new-q-tipo">Tipo da Pergunta</label>
              <select
                id="new-q-tipo"
                value={newTipo}
                onChange={(e) => setNewTipo(e.target.value as TipoPergunta)}
              >
                <option value="RespostaCurta">Resposta Curta</option>
                <option value="TextoLongo">Texto Longo</option>
                <option value="MultiplaEscolha">Múltipla Escolha</option>
                <option value="CaixaDeSelecao">Caixa de Seleção</option>
              </select>
            </div>
          </div>

          <label className="checkbox-inline">
            <input
              type="checkbox"
              checked={newObrigatoria}
              onChange={(e) => setNewObrigatoria(e.target.checked)}
            />
            <span>Definir como pergunta obrigatória</span>
          </label>

          {(newTipo === 'MultiplaEscolha' || newTipo === 'CaixaDeSelecao') && (
            <div className="alternatives-panel">
              <span className="small-label">Alternativas Iniciais:</span>
              {newAlternativas.map((altText, idx) => (
                <div key={idx} className="alt-item-row">
                  <span className="alt-bullet">
                    {newTipo === 'MultiplaEscolha' ? '○' : '☐'}
                  </span>
                  <input
                    type="text"
                    placeholder={`Alternativa ${idx + 1}`}
                    value={altText}
                    onChange={(e) =>
                      setNewAlternativas((prev) =>
                        prev.map((item, i) => (i === idx ? e.target.value : item))
                      )
                    }
                  />
                  <button
                    type="button"
                    className="btn-icon-subtle danger"
                    onClick={() =>
                      setNewAlternativas((prev) => prev.filter((_, i) => i !== idx))
                    }
                  >
                    ✕
                  </button>
                </div>
              ))}
              <button
                type="button"
                className="btn-link"
                onClick={() =>
                  setNewAlternativas((prev) => [...prev, `Opção ${prev.length + 1}`])
                }
              >
                + Adicionar outra opção
              </button>
            </div>
          )}

          <div className="card-actions-right">
            <button type="submit" className="btn btn-primary" disabled={addingQuestion}>
              {addingQuestion ? 'Adicionando...' : '+ Cadastrar Pergunta na API'}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
