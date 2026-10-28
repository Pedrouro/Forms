import { useEffect, useState } from 'react';
import type {
  AuthSession,
  FormularioResponseDTO,
  FormSubmissionRecord,
  PerguntaResponseDTO,
} from '../types/api';
import {
  formularioApi,
  getSavedSubmissions,
  perguntaApi,
  saveFormSubmission,
} from '../services/api';

interface FormResponderProps {
  session: AuthSession;
  initialFormId?: number | null;
  onBackToDashboard: () => void;
  onNotify: (msg: string, type?: 'success' | 'error' | 'info') => void;
}

export function FormResponder({
  session,
  initialFormId,
  onBackToDashboard,
  onNotify,
}: FormResponderProps) {
  const [formIdInput, setFormIdInput] = useState(
    initialFormId ? String(initialFormId) : ''
  );
  const [availableForms, setAvailableForms] = useState<FormularioResponseDTO[]>([]);
  const [form, setForm] = useState<FormularioResponseDTO | null>(null);
  const [perguntas, setPerguntas] = useState<PerguntaResponseDTO[]>([]);
  const [loading, setLoading] = useState(false);

  // Answers state: perguntaId -> string or string[]
  const [answers, setAnswers] = useState<Record<number, string | string[]>>({});
  const [submittedSuccess, setSubmittedSuccess] = useState(false);

  // View tab: 'fill' or 'responses'
  const [viewMode, setViewMode] = useState<'fill' | 'responses'>('fill');
  const [submissions, setSubmissions] = useState<FormSubmissionRecord[]>([]);

  useEffect(() => {
    void formularioApi
      .getAccessibleForms(session.perfil === 'Administrador')
      .then(setAvailableForms)
      .catch(() => {});
  }, [session.perfil]);

  const loadTargetForm = async (targetId: number) => {
    if (!targetId || targetId <= 0) return;
    setLoading(true);
    setSubmittedSuccess(false);
    try {
      const formData = await formularioApi.getById(targetId);
      setForm(formData);
      setFormIdInput(String(targetId));

      let qs: PerguntaResponseDTO[] = [];
      try {
        qs = await perguntaApi.getByFormulario(targetId);
      } catch {
        qs = formData.perguntas || [];
      }
      const sorted = qs.sort((a, b) => a.ordem - b.ordem);
      setPerguntas(sorted);
      setAnswers({});
      setSubmissions(getSavedSubmissions(targetId));
    } catch (err) {
      const msg =
        err instanceof Error
          ? err.message
          : 'Não foi possível carregar o formulário informado.';
      onNotify(msg, 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (initialFormId && initialFormId > 0) {
      void loadTargetForm(initialFormId);
    }
  }, [initialFormId]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const id = Number(formIdInput);
    if (!id || id <= 0) {
      onNotify('Digite um ID válido de formulário.', 'error');
      return;
    }
    void loadTargetForm(id);
  };

  const handleCheckboxToggle = (perguntaId: number, optionText: string) => {
    setAnswers((prev) => {
      const current = prev[perguntaId];
      const arr = Array.isArray(current) ? [...current] : [];
      const exists = arr.includes(optionText);
      const next = exists
        ? arr.filter((item) => item !== optionText)
        : [...arr, optionText];
      return { ...prev, [perguntaId]: next };
    });
  };

  const handleSubmitAnswers = (e: React.FormEvent) => {
    e.preventDefault();
    if (!form) return;

    if (form.status === 'Encerrado') {
      onNotify('Este formulário já foi encerrado e não aceita novas respostas.', 'error');
      return;
    }

    // Validate required questions
    for (const q of perguntas) {
      if (q.obrigatoria) {
        const val = answers[q.id];
        const isEmpty =
          val === undefined ||
          (typeof val === 'string' && val.trim() === '') ||
          (Array.isArray(val) && val.length === 0);
        if (isEmpty) {
          onNotify(`A pergunta "${q.enunciado}" é obrigatória.`, 'error');
          return;
        }
      }
    }

    saveFormSubmission({
      formularioId: form.id,
      usuarioEmail: session.email,
      respostas: answers,
    });

    setSubmissions(getSavedSubmissions(form.id));
    setSubmittedSuccess(true);
    onNotify('Respostas enviadas com sucesso!', 'success');
  };

  return (
    <div className="page-container">
      <div className="builder-topbar">
        <button
          type="button"
          className="btn btn-outline btn-sm"
          onClick={onBackToDashboard}
        >
          ← Voltar aos Formulários
        </button>

        <form className="responder-selector" onSubmit={handleSearchSubmit}>
          <select
            value={form?.id ? String(form.id) : ''}
            onChange={(e) => {
              const val = Number(e.target.value);
              if (val > 0) void loadTargetForm(val);
            }}
          >
            <option value="">Selecionar formulário recente...</option>
            {availableForms.map((f) => (
              <option key={f.id} value={f.id}>
                #{f.id} - {f.titulo} ({f.status})
              </option>
            ))}
          </select>

          <input
            type="number"
            min="1"
            placeholder="ID do formulário"
            value={formIdInput}
            onChange={(e) => setFormIdInput(e.target.value)}
          />
          <button type="submit" className="btn btn-primary btn-sm">
            Carregar
          </button>
        </form>
      </div>

      {loading ? (
        <div className="empty-state">
          <div className="spinner" />
          <p>Carregando formulário para preenchimento...</p>
        </div>
      ) : !form ? (
        <div className="empty-state">
          <h3>Área Exclusiva de Preenchimento</h3>
          <p>
            Selecione um formulário acima ou informe o ID recebido no link de compartilhamento
            para responder às perguntas.
          </p>
        </div>
      ) : (
        <div className="responder-container">
          <div className="responder-tabs">
            <button
              type="button"
              className={`auth-tab ${viewMode === 'fill' ? 'active' : ''}`}
              onClick={() => setViewMode('fill')}
            >
              Preencher Formulário
            </button>
            <button
              type="button"
              className={`auth-tab ${viewMode === 'responses' ? 'active' : ''}`}
              onClick={() => setViewMode('responses')}
            >
              Respostas Recebidas ({submissions.length})
            </button>
          </div>

          {viewMode === 'fill' ? (
            submittedSuccess ? (
              <div className="responder-success-card">
                <div className="success-circle">✓</div>
                <h2>Sua resposta foi registrada!</h2>
                <p>
                  Obrigado por responder ao formulário <strong>{form.titulo}</strong>.
                </p>
                <div className="success-actions">
                  <button
                    type="button"
                    className="btn btn-primary"
                    onClick={() => {
                      setAnswers({});
                      setSubmittedSuccess(false);
                    }}
                  >
                    Enviar outra resposta
                  </button>
                  <button
                    type="button"
                    className="btn btn-outline"
                    onClick={() => setViewMode('responses')}
                  >
                    Ver Respostas ({submissions.length})
                  </button>
                </div>
              </div>
            ) : (
              <form className="responder-form" onSubmit={handleSubmitAnswers}>
                <div className="responder-header-card">
                  <div className="builder-header-strip" />
                  <div className="responder-header-body">
                    <div className="section-row-between">
                      <span className="id-badge">Formulário #{form.id}</span>
                      <span className={`status-badge status-${form.status.toLowerCase()}`}>
                        {form.status}
                      </span>
                    </div>
                    <h1>{form.titulo}</h1>
                    {form.descricao && <p className="responder-desc">{form.descricao}</p>}
                    <p className="required-legend">
                      Campos marcados com <span className="req-star">*</span> são obrigatórios
                    </p>
                  </div>
                </div>

                {form.status === 'Encerrado' ? (
                  <div className="alert alert-error">
                    O recebimento de respostas para o formulário &ldquo;{form.titulo}&rdquo; foi
                    encerrado pelo criador.
                  </div>
                ) : perguntas.length === 0 ? (
                  <div className="empty-card">
                    <p>Este formulário ainda não possui perguntas cadastradas.</p>
                  </div>
                ) : (
                  <>
                    {perguntas.map((q, index) => (
                      <div key={q.id} className="responder-question-card">
                        <label className="responder-question-label">
                          <span className="q-index">{index + 1}.</span> {q.enunciado}
                          {q.obrigatoria && <span className="req-star"> *</span>}
                        </label>

                        {q.tipo === 'RespostaCurta' && (
                          <input
                            type="text"
                            className="responder-input"
                            placeholder="Sua resposta..."
                            required={q.obrigatoria}
                            value={(answers[q.id] as string) || ''}
                            onChange={(e) =>
                              setAnswers((prev) => ({ ...prev, [q.id]: e.target.value }))
                            }
                          />
                        )}

                        {q.tipo === 'TextoLongo' && (
                          <textarea
                            rows={4}
                            className="responder-textarea"
                            placeholder="Digite sua resposta detalhada..."
                            required={q.obrigatoria}
                            value={(answers[q.id] as string) || ''}
                            onChange={(e) =>
                              setAnswers((prev) => ({ ...prev, [q.id]: e.target.value }))
                            }
                          />
                        )}

                        {q.tipo === 'MultiplaEscolha' && (
                          <div className="responder-options">
                            {(q.alternativas || []).map((alt) => (
                              <label key={alt.id} className="option-row">
                                <input
                                  type="radio"
                                  name={`pergunta-${q.id}`}
                                  value={alt.texto}
                                  checked={answers[q.id] === alt.texto}
                                  onChange={() =>
                                    setAnswers((prev) => ({ ...prev, [q.id]: alt.texto }))
                                  }
                                />
                                <span>{alt.texto}</span>
                              </label>
                            ))}
                          </div>
                        )}

                        {q.tipo === 'CaixaDeSelecao' && (
                          <div className="responder-options">
                            {(q.alternativas || []).map((alt) => {
                              const selectedList = Array.isArray(answers[q.id])
                                ? (answers[q.id] as string[])
                                : [];
                              return (
                                <label key={alt.id} className="option-row">
                                  <input
                                    type="checkbox"
                                    checked={selectedList.includes(alt.texto)}
                                    onChange={() => handleCheckboxToggle(q.id, alt.texto)}
                                  />
                                  <span>{alt.texto}</span>
                                </label>
                              );
                            })}
                          </div>
                        )}
                      </div>
                    ))}

                    <div className="responder-submit-bar">
                      <button
                        type="button"
                        className="btn btn-outline"
                        onClick={() => setAnswers({})}
                      >
                        Limpar Formulário
                      </button>
                      <button type="submit" className="btn btn-primary">
                        Enviar Respostas
                      </button>
                    </div>
                  </>
                )}
              </form>
            )
          ) : (
            <div className="responses-list-section">
              {submissions.length === 0 ? (
                <div className="empty-card">
                  <p>Nenhuma resposta registrada para este formulário até o momento.</p>
                </div>
              ) : (
                submissions.map((sub, idx) => (
                  <div key={sub.id} className="submission-card">
                    <div className="submission-header">
                      <strong>Resposta #{submissions.length - idx}</strong>
                      <span>
                        Enviado por <em>{sub.usuarioEmail}</em> em{' '}
                        {new Date(sub.dataEnvio).toLocaleString('pt-BR')}
                      </span>
                    </div>
                    <div className="submission-answers">
                      {perguntas.map((q) => {
                        const val = sub.respostas[q.id];
                        const formatted = Array.isArray(val)
                          ? val.join(', ')
                          : val || '—';
                        return (
                          <div key={q.id} className="submission-answer-item">
                            <span className="sub-q">{q.enunciado}</span>
                            <strong className="sub-a">{formatted}</strong>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
