import type { AuthSession } from '../types/api';

export type ActiveTab = 'dashboard' | 'builder' | 'responder' | 'users' | 'profile';

interface NavbarProps {
  session: AuthSession;
  activeTab: ActiveTab;
  onSelectTab: (tab: ActiveTab) => void;
  onLogout: () => void;
}

export function Navbar({
  session,
  activeTab,
  onSelectTab,
  onLogout,
}: NavbarProps) {
  return (
    <header className="navbar">
      <div className="navbar-container">
        <div
          className="navbar-brand"
          onClick={() => onSelectTab('dashboard')}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => e.key === 'Enter' && onSelectTab('dashboard')}
        >
          <div className="brand-logo">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M9 11l3 3L22 4" />
              <path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11" />
            </svg>
          </div>
          <div className="brand-text">
            <span className="brand-title">ProjetoForms</span>
            <span className="brand-subtitle">Gestão de Formulários</span>
          </div>
        </div>

        <nav className="navbar-links">
          <button
            type="button"
            className={`nav-link ${activeTab === 'dashboard' || activeTab === 'builder' ? 'active' : ''}`}
            onClick={() => onSelectTab('dashboard')}
          >
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <rect x="3" y="3" width="7" height="7" rx="1" />
              <rect x="14" y="3" width="7" height="7" rx="1" />
              <rect x="14" y="14" width="7" height="7" rx="1" />
              <rect x="3" y="14" width="7" height="7" rx="1" />
            </svg>
            Formulários
          </button>

          <button
            type="button"
            className={`nav-link ${activeTab === 'responder' ? 'active' : ''}`}
            onClick={() => onSelectTab('responder')}
          >
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
              <line x1="16" y1="13" x2="8" y2="13" />
              <line x1="16" y1="17" x2="8" y2="17" />
            </svg>
            Área de Preenchimento
          </button>

          {session.perfil === 'Administrador' && (
            <button
              type="button"
              className={`nav-link ${activeTab === 'users' ? 'active' : ''}`}
              onClick={() => onSelectTab('users')}
            >
              <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                <circle cx="9" cy="7" r="4" />
                <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                <path d="M16 3.13a4 4 0 0 1 0 7.75" />
              </svg>
              Usuários
            </button>
          )}

          <button
            type="button"
            className={`nav-link ${activeTab === 'profile' ? 'active' : ''}`}
            onClick={() => onSelectTab('profile')}
          >
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
            Meu Perfil
          </button>
        </nav>

        <div className="navbar-actions">
          <div className="user-pill">
            <div className="user-avatar">
              {(session.nome || session.email || 'U').charAt(0).toUpperCase()}
            </div>
            <div className="user-meta">
              <span className="user-name">{session.nome || session.email}</span>
              <span className={`role-badge ${session.perfil === 'Administrador' ? 'admin' : 'comum'}`}>
                {session.perfil}
              </span>
            </div>
          </div>

          <button type="button" className="btn-logout" onClick={onLogout} title="Sair da conta">
            <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
              <polyline points="16 17 21 12 16 7" />
              <line x1="21" y1="12" x2="9" y2="12" />
            </svg>
            <span>Sair</span>
          </button>
        </div>
      </div>
    </header>
  );
}
