import { useEffect, useState } from 'react';
import type { AuthSession } from './types/api';
import { authApi, getSavedSession } from './services/api';
import { Navbar, type ActiveTab } from './components/Navbar';
import { AuthView } from './components/AuthView';
import { FormsDashboard } from './components/FormsDashboard';
import { FormBuilder } from './components/FormBuilder';
import { FormResponder } from './components/FormResponder';
import { UsersView } from './components/UsersView';
import './App.css';

interface ToastMessage {
  id: number;
  text: string;
  type: 'success' | 'error' | 'info';
}

export function App() {
  const [session, setSession] = useState<AuthSession | null>(() => getSavedSession());
  const [activeTab, setActiveTab] = useState<ActiveTab>('dashboard');
  const [selectedFormId, setSelectedFormId] = useState<number | null>(null);
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const notify = (text: string, type: 'success' | 'error' | 'info' = 'info') => {
    const id = Date.now() + Math.floor(Math.random() * 1000);
    setToasts((prev) => [...prev, { id, text, type }]);
    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, 4500);
  };

  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const responderParam = params.get('responder');
    if (responderParam) {
      const id = Number(responderParam);
      if (id > 0) {
        setSelectedFormId(id);
        setActiveTab('responder');
      }
    }
  }, []);

  const handleLogout = () => {
    authApi.logout();
    setSession(null);
    setActiveTab('dashboard');
    notify('Sessão encerrada com sucesso.', 'info');
  };

  const handleEditForm = (formId: number) => {
    setSelectedFormId(formId);
    setActiveTab('builder');
  };

  const handleRespondForm = (formId: number) => {
    setSelectedFormId(formId);
    setActiveTab('responder');
  };

  return (
    <div className="app-root">
      {/* Toast Notifications */}
      <div className="toast-stack" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast toast-${t.type}`}>
            <span>{t.text}</span>
            <button
              type="button"
              className="toast-close"
              onClick={() => setToasts((prev) => prev.filter((item) => item.id !== t.id))}
            >
              ✕
            </button>
          </div>
        ))}
      </div>

      {!session ? (
        <AuthView
          onAuthenticated={(newSession) => setSession(newSession)}
          onNotify={notify}
        />
      ) : (
        <>
          <Navbar
            session={session}
            activeTab={activeTab}
            onSelectTab={(tab) => setActiveTab(tab)}
            onLogout={handleLogout}
          />

          <main className="main-content">
            {activeTab === 'dashboard' && (
              <FormsDashboard
                session={session}
                onEditForm={handleEditForm}
                onRespondForm={handleRespondForm}
                onNotify={notify}
              />
            )}

            {activeTab === 'builder' && selectedFormId && (
              <FormBuilder
                formId={selectedFormId}
                onBack={() => setActiveTab('dashboard')}
                onPreview={(id) => handleRespondForm(id)}
                onNotify={notify}
              />
            )}

            {activeTab === 'responder' && (
              <FormResponder
                session={session}
                initialFormId={selectedFormId}
                onBackToDashboard={() => setActiveTab('dashboard')}
                onNotify={notify}
              />
            )}

            {activeTab === 'users' && session.perfil === 'Administrador' && (
              <UsersView
                session={session}
                mode="admin-users"
                onSessionUpdated={setSession}
                onNotify={notify}
              />
            )}

            {activeTab === 'profile' && (
              <UsersView
                session={session}
                mode="profile"
                onSessionUpdated={setSession}
                onNotify={notify}
              />
            )}
          </main>

          <footer className="app-footer">
            <span>ProjetoForms Web • Interface React integrada à API ASP.NET Core</span>
          </footer>
        </>
      )}
    </div>
  );
}

export default App;
