import { useEffect, useState } from 'react';
import type { AuthSession, PerfilUsuario, UsuarioModel } from '../types/api';
import { saveSession, usuarioApi } from '../services/api';

interface UsersViewProps {
  session: AuthSession;
  mode: 'profile' | 'admin-users';
  onSessionUpdated: (session: AuthSession) => void;
  onNotify: (msg: string, type?: 'success' | 'error' | 'info') => void;
}

export function UsersView({
  session,
  mode,
  onSessionUpdated,
  onNotify,
}: UsersViewProps) {
  // Profile State
  const [profileNome, setProfileNome] = useState(session.nome || '');
  const [profileEmail, setProfileEmail] = useState(session.email || '');
  const [profileSenha, setProfileSenha] = useState('');
  const [profilePerfil, setProfilePerfil] = useState<PerfilUsuario>(session.perfil);
  const [loadingProfile, setLoadingProfile] = useState(false);
  const [savingProfile, setSavingProfile] = useState(false);

  // Admin Users List State
  const [users, setUsers] = useState<UsuarioModel[]>([]);
  const [loadingUsers, setLoadingUsers] = useState(false);

  // Admin Create/Edit Modal State
  const [showUserModal, setShowUserModal] = useState(false);
  const [editingUser, setEditingUser] = useState<UsuarioModel | null>(null);
  const [uNome, setUNome] = useState('');
  const [uEmail, setUEmail] = useState('');
  const [uSenha, setUSenha] = useState('');
  const [uPerfil, setUPerfil] = useState<PerfilUsuario>('Comum');
  const [savingUser, setSavingUser] = useState(false);

  const loadMyProfile = async () => {
    if (!session.userId) return;
    setLoadingProfile(true);
    try {
      const data = await usuarioApi.getById(session.userId);
      setProfileNome(data.nome);
      setProfileEmail(data.email);
      setProfilePerfil(data.perfil);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao buscar dados do perfil.';
      onNotify(msg, 'error');
    } finally {
      setLoadingProfile(false);
    }
  };

  const loadAllUsers = async () => {
    setLoadingUsers(true);
    try {
      const list = await usuarioApi.getAll();
      setUsers(list);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao listar usuários.';
      onNotify(msg, 'error');
    } finally {
      setLoadingUsers(false);
    }
  };

  useEffect(() => {
    if (mode === 'profile') {
      void loadMyProfile();
    } else {
      void loadAllUsers();
    }
  }, [mode, session.userId]);

  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!profileSenha.trim()) {
      onNotify('Informe sua senha atual ou uma nova senha para confirmar a alteração.', 'error');
      return;
    }
    setSavingProfile(true);
    try {
      const res = await usuarioApi.update(session.userId, {
        nome: profileNome.trim(),
        email: profileEmail.trim(),
        senha: profileSenha,
        perfil: profilePerfil,
      });
      onNotify(res.message || 'Perfil atualizado com sucesso!', 'success');
      setProfileSenha('');

      const updatedSession: AuthSession = {
        ...session,
        nome: profileNome.trim(),
        email: profileEmail.trim(),
        perfil: profilePerfil,
      };
      saveSession(updatedSession);
      onSessionUpdated(updatedSession);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao atualizar perfil.';
      onNotify(msg, 'error');
    } finally {
      setSavingProfile(false);
    }
  };

  const openCreateUserModal = () => {
    setEditingUser(null);
    setUNome('');
    setUEmail('');
    setUSenha('');
    setUPerfil('Comum');
    setShowUserModal(true);
  };

  const openEditUserModal = (u: UsuarioModel) => {
    setEditingUser(u);
    setUNome(u.nome);
    setUEmail(u.email);
    setUSenha('');
    setUPerfil(u.perfil);
    setShowUserModal(true);
  };

  const handleSaveAdminUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!uSenha.trim()) {
      onNotify('A senha é obrigatória.', 'error');
      return;
    }
    setSavingUser(true);
    try {
      if (editingUser) {
        const res = await usuarioApi.update(editingUser.id, {
          nome: uNome.trim(),
          email: uEmail.trim(),
          senha: uSenha,
          perfil: uPerfil,
        });
        onNotify(res.message || 'Usuário atualizado!', 'success');
      } else {
        const res = await usuarioApi.create({
          nome: uNome.trim(),
          email: uEmail.trim(),
          senha: uSenha,
        });
        onNotify(res.message || 'Usuário cadastrado!', 'success');
      }
      setShowUserModal(false);
      await loadAllUsers();
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao salvar usuário.';
      onNotify(msg, 'error');
    } finally {
      setSavingUser(false);
    }
  };

  const handleDeleteUser = async (u: UsuarioModel) => {
    if (!window.confirm(`Deseja realmente remover o usuário "${u.nome}" (${u.email})?`)) {
      return;
    }
    try {
      const msg = await usuarioApi.remove(u.id);
      onNotify(msg, 'success');
      setUsers((prev) => prev.filter((item) => item.id !== u.id));
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Erro ao excluir usuário.';
      onNotify(msg, 'error');
    }
  };

  if (mode === 'profile') {
    return (
      <div className="page-container">
        <div className="page-header">
          <div>
            <h1 className="page-title">Meu Perfil</h1>
            <p className="page-subtitle">
              Visualize e atualize suas informações cadastrais na API (`/api/Usuario/{'{id}'}`).
            </p>
          </div>
        </div>

        <div className="profile-card">
          {loadingProfile ? (
            <div className="empty-state">
              <div className="spinner" />
              <p>Carregando dados do usuário...</p>
            </div>
          ) : (
            <form onSubmit={handleSaveProfile} className="auth-form">
              <div className="profile-summary">
                <div className="profile-avatar-lg">
                  {(profileNome || profileEmail || 'U').charAt(0).toUpperCase()}
                </div>
                <div>
                  <h3>{profileNome || 'Usuário'}</h3>
                  <span>
                    ID #{session.userId} • Perfil {profilePerfil}
                  </span>
                </div>
              </div>

              <div className="form-group">
                <label htmlFor="prof-nome">Nome Completo</label>
                <input
                  id="prof-nome"
                  type="text"
                  required
                  value={profileNome}
                  onChange={(e) => setProfileNome(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label htmlFor="prof-email">E-mail</label>
                <input
                  id="prof-email"
                  type="email"
                  required
                  value={profileEmail}
                  onChange={(e) => setProfileEmail(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label htmlFor="prof-perfil">Perfil</label>
                <select
                  id="prof-perfil"
                  value={profilePerfil}
                  onChange={(e) => setProfilePerfil(e.target.value as PerfilUsuario)}
                >
                  <option value="Comum">Comum</option>
                  <option value="Administrador">Administrador</option>
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="prof-senha">Senha (Atual ou Nova Senha) *</label>
                <input
                  id="prof-senha"
                  type="password"
                  required
                  placeholder="Digite sua senha para confirmar ou alterar"
                  value={profileSenha}
                  onChange={(e) => setProfileSenha(e.target.value)}
                />
              </div>

              <div className="card-actions-right">
                <button type="submit" className="btn btn-primary" disabled={savingProfile}>
                  {savingProfile ? 'Salvando...' : 'Atualizar Meus Dados'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <div className="page-header">
        <div>
          <h1 className="page-title">Gestão de Usuários</h1>
          <p className="page-subtitle">
            Administração de contas cadastradas no sistema (`/api/Usuario`).
          </p>
        </div>
        <div className="header-actions">
          <button
            type="button"
            className="btn btn-outline"
            onClick={() => void loadAllUsers()}
            disabled={loadingUsers}
          >
            Atualizar Lista
          </button>
          <button type="button" className="btn btn-primary" onClick={openCreateUserModal}>
            + Novo Usuário
          </button>
        </div>
      </div>

      {loadingUsers ? (
        <div className="empty-state">
          <div className="spinner" />
          <p>Carregando lista de usuários...</p>
        </div>
      ) : (
        <div className="table-card">
          <table className="clean-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Nome</th>
                <th>E-mail</th>
                <th>Perfil</th>
                <th className="text-right">Ações</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id}>
                  <td>
                    <span className="id-badge">#{u.id}</span>
                  </td>
                  <td>
                    <strong>{u.nome}</strong>
                  </td>
                  <td>{u.email}</td>
                  <td>
                    <span
                      className={`role-badge ${
                        u.perfil === 'Administrador' ? 'admin' : 'comum'
                      }`}
                    >
                      {u.perfil}
                    </span>
                  </td>
                  <td className="text-right">
                    <div className="table-actions">
                      <button
                        type="button"
                        className="btn btn-outline btn-sm"
                        onClick={() => openEditUserModal(u)}
                      >
                        Editar
                      </button>
                      <button
                        type="button"
                        className="btn-icon-subtle danger"
                        title="Excluir usuário"
                        onClick={() => void handleDeleteUser(u)}
                      >
                        Excluir
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showUserModal && (
        <div className="modal-backdrop" onClick={() => setShowUserModal(false)}>
          <div className="modal-panel" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{editingUser ? `Editar Usuário #${editingUser.id}` : 'Novo Usuário'}</h2>
              <button
                type="button"
                className="btn-icon-subtle"
                onClick={() => setShowUserModal(false)}
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleSaveAdminUser} className="modal-body">
              <div className="form-group">
                <label htmlFor="admin-u-nome">Nome</label>
                <input
                  id="admin-u-nome"
                  type="text"
                  required
                  value={uNome}
                  onChange={(e) => setUNome(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label htmlFor="admin-u-email">E-mail</label>
                <input
                  id="admin-u-email"
                  type="email"
                  required
                  value={uEmail}
                  onChange={(e) => setUEmail(e.target.value)}
                />
              </div>

              {editingUser && (
                <div className="form-group">
                  <label htmlFor="admin-u-perfil">Perfil</label>
                  <select
                    id="admin-u-perfil"
                    value={uPerfil}
                    onChange={(e) => setUPerfil(e.target.value as PerfilUsuario)}
                  >
                    <option value="Comum">Comum</option>
                    <option value="Administrador">Administrador</option>
                  </select>
                </div>
              )}

              <div className="form-group">
                <label htmlFor="admin-u-senha">Senha *</label>
                <input
                  id="admin-u-senha"
                  type="password"
                  required
                  placeholder={editingUser ? 'Nova senha ou senha atual' : 'Defina a senha'}
                  value={uSenha}
                  onChange={(e) => setUSenha(e.target.value)}
                />
              </div>

              <div className="modal-footer">
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={() => setShowUserModal(false)}
                >
                  Cancelar
                </button>
                <button type="submit" className="btn btn-primary" disabled={savingUser}>
                  {savingUser ? 'Salvando...' : 'Salvar Usuário'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
