import { useState } from 'react';
import type { AuthSession } from '../types/api';
import { authApi, usuarioApi } from '../services/api';

interface AuthViewProps {
  onAuthenticated: (session: AuthSession) => void;
  onNotify: (msg: string, type?: 'success' | 'error' | 'info') => void;
}

export function AuthView({ onAuthenticated, onNotify }: AuthViewProps) {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Login fields
  const [loginEmail, setLoginEmail] = useState('');
  const [loginSenha, setLoginSenha] = useState('');

  // Register fields
  const [regNome, setRegNome] = useState('');
  const [regEmail, setRegEmail] = useState('');
  const [regSenha, setRegSenha] = useState('');

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const session = await authApi.login({
        email: loginEmail.trim(),
        senha: loginSenha,
      });
      onNotify(`Bem-vindo(a), ${session.nome || session.email}!`, 'success');
      onAuthenticated(session);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Falha ao realizar login.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const res = await usuarioApi.create({
        nome: regNome.trim(),
        email: regEmail.trim(),
        senha: regSenha,
      });
      onNotify(res.message || 'Conta criada com sucesso! Realizando login...', 'success');

      // Auto-login after registration
      const session = await authApi.login({
        email: regEmail.trim(),
        senha: regSenha,
      });
      onAuthenticated(session);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Não foi possível concluir o cadastro.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-wrapper">
      <div className="auth-shell">
        <div className="auth-Showcase">
          <div className="showcase-badge">Plataforma de Formulários</div>
          <h1>Crie, compartilhe e gerencie formulários com simplicidade.</h1>
          <p>
            Construa questionários dinâmicos com perguntas de resposta curta, texto longo,
            múltipla escolha e caixas de seleção, integrados diretamente à API ProjetoForms.
          </p>

          <div className="showcase-features">
            <div className="showcase-feature-item">
              <div className="feature-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M12 20h9" />
                  <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" />
                </svg>
              </div>
              <div>
                <strong>Editor Completo</strong>
                <span>Crie e ordene perguntas e alternativas em tempo real</span>
              </div>
            </div>

            <div className="showcase-feature-item">
              <div className="feature-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <circle cx="18" cy="5" r="3" />
                  <circle cx="6" cy="12" r="3" />
                  <circle cx="18" cy="19" r="3" />
                  <line x1="8.59" y1="13.51" x2="15.42" y2="17.49" />
                  <line x1="15.41" y1="6.51" x2="8.59" y2="10.49" />
                </svg>
              </div>
              <div>
                <strong>Compartilhamento Rápido</strong>
                <span>Gere links diretos para a área exclusiva de preenchimento</span>
              </div>
            </div>

            <div className="showcase-feature-item">
              <div className="feature-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                  <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                </svg>
              </div>
              <div>
                <strong>Segurança JWT & Perfis</strong>
                <span>Autenticação Bearer com controle para usuários Comuns e Administradores</span>
              </div>
            </div>
          </div>
        </div>

        <div className="auth-card">
          <div className="auth-tabs">
            <button
              type="button"
              className={`auth-tab ${mode === 'login' ? 'active' : ''}`}
              onClick={() => {
                setMode('login');
                setError(null);
              }}
            >
              Entrar
            </button>
            <button
              type="button"
              className={`auth-tab ${mode === 'register' ? 'active' : ''}`}
              onClick={() => {
                setMode('register');
                setError(null);
              }}
            >
              Criar Conta
            </button>
          </div>

          {error && (
            <div className="alert alert-error">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <circle cx="12" cy="12" r="10" />
                <line x1="12" y1="8" x2="12" y2="12" />
                <line x1="12" y1="16" x2="12.01" y2="16" />
              </svg>
              <span>{error}</span>
            </div>
          )}

          {mode === 'login' ? (
            <form onSubmit={handleLogin} className="auth-form">
              <div className="auth-header">
                <h2>Acesse sua conta</h2>
                <p>Informe suas credenciais para gerenciar seus formulários</p>
              </div>

              <div className="form-group">
                <label htmlFor="login-email">E-mail</label>
                <input
                  id="login-email"
                  type="email"
                  required
                  placeholder="seuemail@exemplo.com"
                  value={loginEmail}
                  onChange={(e) => setLoginEmail(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label htmlFor="login-senha">Senha</label>
                <input
                  id="login-senha"
                  type="password"
                  required
                  placeholder="Digite sua senha"
                  value={loginSenha}
                  onChange={(e) => setLoginSenha(e.target.value)}
                />
              </div>

              <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
                {loading ? 'Autenticando...' : 'Entrar no Sistema'}
              </button>
            </form>
          ) : (
            <form onSubmit={handleRegister} className="auth-form">
              <div className="auth-header">
                <h2>Novo Cadastro</h2>
                <p>Crie sua conta gratuitamente para utilizar o ProjetoForms</p>
              </div>

              <div className="form-group">
                <label htmlFor="reg-nome">Nome Completo</label>
                <input
                  id="reg-nome"
                  type="text"
                  required
                  placeholder="Ex: Maria Silva"
                  value={regNome}
                  onChange={(e) => setRegNome(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label htmlFor="reg-email">E-mail</label>
                <input
                  id="reg-email"
                  type="email"
                  required
                  placeholder="seuemail@exemplo.com"
                  value={regEmail}
                  onChange={(e) => setRegEmail(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label htmlFor="reg-senha">Senha</label>
                <input
                  id="reg-senha"
                  type="password"
                  required
                  placeholder="Crie uma senha segura"
                  value={regSenha}
                  onChange={(e) => setRegSenha(e.target.value)}
                />
              </div>

              <button type="submit" className="btn btn-primary btn-block" disabled={loading}>
                {loading ? 'Cadastrando...' : 'Criar Conta e Acessar'}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
