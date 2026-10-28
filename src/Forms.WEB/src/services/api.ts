import type {
  AlternativaDTO,
  AlternativaResponseDTO,
  AuthSession,
  FormularioDTO,
  FormularioResponseDTO,
  FormSubmissionRecord,
  LoginDTO,
  PerfilUsuario,
  PerguntaDTO,
  PerguntaResponseDTO,
  ResponseDTO,
  UsuarioDTO,
  UsuarioModel,
} from '../types/api';

const STORAGE_SESSION_KEY = 'forms_web_session';
const STORAGE_KNOWN_FORMS_KEY = 'forms_web_known_form_ids';
const STORAGE_SUBMISSIONS_KEY = 'forms_web_submissions';

function getApiBaseUrl(): string {
  const envUrl = import.meta.env.VITE_API_URL as string | undefined;
  if (envUrl && envUrl.trim()) {
    return envUrl.trim().replace(/\/+$/, '');
  }
  return '/api';
}

export function getSavedSession(): AuthSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_SESSION_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as AuthSession;
    if (!parsed?.token) return null;
    return parsed;
  } catch {
    return null;
  }
}

export function saveSession(session: AuthSession | null): void {
  if (!session) {
    localStorage.removeItem(STORAGE_SESSION_KEY);
  } else {
    localStorage.setItem(STORAGE_SESSION_KEY, JSON.stringify(session));
  }
}

export function parseJwtToken(rawToken: string): Omit<AuthSession, 'nome'> {
  const cleanToken = rawToken.replace(/^Bearer:\s*/i, '').replace(/^Bearer\s+/i, '').trim();
  const parts = cleanToken.split('.');
  if (parts.length < 2) {
    throw new Error('Token JWT inválido recebido do servidor.');
  }

  const base64Url = parts[1];
  const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  const jsonPayload = decodeURIComponent(
    atob(padded)
      .split('')
      .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
      .join('')
  );

  const payload = JSON.parse(jsonPayload) as Record<string, unknown>;

  const rawId =
    payload['nameid'] ??
    payload['sub'] ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

  const rawEmail =
    payload['email'] ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ??
    '';

  const rawRole =
    payload['role'] ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'] ??
    'Comum';

  const perfil: PerfilUsuario =
    String(rawRole) === 'Administrador' || String(rawRole) === '1'
      ? 'Administrador'
      : 'Comum';

  return {
    token: cleanToken,
    userId: Number(rawId) || 0,
    email: String(rawEmail),
    perfil,
  };
}

export function getKnownFormIds(): number[] {
  try {
    const raw = localStorage.getItem(STORAGE_KNOWN_FORMS_KEY);
    if (!raw) return [];
    const ids = JSON.parse(raw) as number[];
    return Array.isArray(ids) ? Array.from(new Set(ids.filter((id) => typeof id === 'number' && id > 0))) : [];
  } catch {
    return [];
  }
}

export function addKnownFormId(id: number): void {
  if (!id || id <= 0) return;
  const current = getKnownFormIds();
  if (!current.includes(id)) {
    current.unshift(id);
    localStorage.setItem(STORAGE_KNOWN_FORMS_KEY, JSON.stringify(current));
  }
}

export function removeKnownFormId(id: number): void {
  const current = getKnownFormIds().filter((item) => item !== id);
  localStorage.setItem(STORAGE_KNOWN_FORMS_KEY, JSON.stringify(current));
}

export function getSavedSubmissions(formularioId?: number): FormSubmissionRecord[] {
  try {
    const raw = localStorage.getItem(STORAGE_SUBMISSIONS_KEY);
    if (!raw) return [];
    const list = JSON.parse(raw) as FormSubmissionRecord[];
    if (!Array.isArray(list)) return [];
    return formularioId ? list.filter((s) => s.formularioId === formularioId) : list;
  } catch {
    return [];
  }
}

export function saveFormSubmission(record: Omit<FormSubmissionRecord, 'id' | 'dataEnvio'>): FormSubmissionRecord {
  const all = getSavedSubmissions();
  const created: FormSubmissionRecord = {
    ...record,
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    dataEnvio: new Date().toISOString(),
  };
  all.unshift(created);
  localStorage.setItem(STORAGE_SUBMISSIONS_KEY, JSON.stringify(all));
  return created;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const baseUrl = getApiBaseUrl();
  const url = `${baseUrl}${path.startsWith('/') ? path : `/${path}`}`;
  const session = getSavedSession();

  const headers = new Headers(options.headers || {});
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }
  if (session?.token) {
    headers.set('Authorization', `Bearer ${session.token}`);
  }

  const response = await fetch(url, {
    ...options,
    headers,
  });

  const contentType = response.headers.get('content-type') || '';
  let body: unknown = null;

  if (contentType.includes('application/json')) {
    body = await response.json().catch(() => null);
  } else {
    const text = await response.text().catch(() => '');
    try {
      body = text ? JSON.parse(text) : text;
    } catch {
      body = text;
    }
  }

  if (!response.ok) {
    if (body && typeof body === 'object') {
      const obj = body as Record<string, unknown>;
      if (typeof obj.message === 'string' && obj.message) {
        throw new Error(obj.message);
      }
      if (typeof obj.Message === 'string' && obj.Message) {
        throw new Error(obj.Message);
      }
      if (obj.errors && typeof obj.errors === 'object') {
        const errMessages = Object.values(obj.errors as Record<string, string[]>)
          .flat()
          .join(' ');
        if (errMessages) throw new Error(errMessages);
      }
      if (typeof obj.title === 'string' && obj.title) {
        throw new Error(obj.title);
      }
    }
    if (response.status === 401) {
      throw new Error('Sessão expirada ou não autorizada (401). Faça login novamente.');
    }
    if (response.status === 403) {
      throw new Error('Você não possui permissão para realizar esta ação (403).');
    }
    if (response.status === 404) {
      throw new Error('Recurso não encontrado (404).');
    }
    throw new Error(`Erro na requisição (${response.status}).`);
  }

  return body as T;
}

// ==================== AUTH CONTROLLER (/api/Auth) ====================
export const authApi = {
  async login(dto: LoginDTO): Promise<AuthSession> {
    const res = await request<ResponseDTO>('/Auth', {
      method: 'POST',
      body: JSON.stringify(dto),
    });

    if (!res.status) {
      throw new Error(res.message || 'Credenciais inválidas.');
    }

    const parsed = parseJwtToken(res.message);
    const session: AuthSession = {
      ...parsed,
    };
    saveSession(session);

    if (session.userId > 0) {
      try {
        const user = await usuarioApi.getById(session.userId);
        session.nome = user.nome;
        session.email = user.email || session.email;
        session.perfil = user.perfil || session.perfil;
        saveSession(session);
      } catch {
        // Keep session from JWT claims if user lookup fails
      }
    }

    return session;
  },

  logout(): void {
    saveSession(null);
  },
};

// ==================== USUARIO CONTROLLER (/api/Usuario) ====================
export const usuarioApi = {
  async create(dto: UsuarioDTO): Promise<ResponseDTO> {
    return request<ResponseDTO>('/Usuario', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  async getById(id: number): Promise<UsuarioModel> {
    return request<UsuarioModel>(`/Usuario/${id}`, {
      method: 'GET',
    });
  },

  async getAll(): Promise<UsuarioModel[]> {
    return request<UsuarioModel[]>('/Usuario', {
      method: 'GET',
    });
  },

  async update(id: number, dto: UsuarioDTO): Promise<ResponseDTO> {
    return request<ResponseDTO>(`/Usuario/${id}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  async remove(id: number): Promise<string> {
    const res = await request<string | ResponseDTO>(`/Usuario/${id}`, {
      method: 'DELETE',
    });
    if (typeof res === 'string') return res;
    return res?.message || 'Usuário removido com sucesso.';
  },
};

// ==================== FORMULARIO CONTROLLER (/api/Formulario) ====================
export const formularioApi = {
  async create(dto: FormularioDTO): Promise<ResponseDTO<FormularioResponseDTO>> {
    const res = await request<ResponseDTO<FormularioResponseDTO>>('/Formulario', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
    if (res.data?.id) {
      addKnownFormId(res.data.id);
    }
    return res;
  },

  async getById(id: number): Promise<FormularioResponseDTO> {
    const form = await request<FormularioResponseDTO>(`/Formulario/${id}`, {
      method: 'GET',
    });
    if (form?.id) {
      addKnownFormId(form.id);
    }
    return form;
  },

  async getAll(): Promise<FormularioResponseDTO[]> {
    const list = await request<FormularioResponseDTO[]>('/Formulario', {
      method: 'GET',
    });
    list.forEach((f) => addKnownFormId(f.id));
    return list;
  },

  async getAccessibleForms(isAdmin: boolean): Promise<FormularioResponseDTO[]> {
    if (isAdmin) {
      try {
        return await this.getAll();
      } catch {
        // Fallback to known IDs if /Formulario fails
      }
    }

    const knownIds = getKnownFormIds();
    const results: FormularioResponseDTO[] = [];

    await Promise.all(
      knownIds.map(async (id) => {
        try {
          const form = await this.getById(id);
          if (form) results.push(form);
        } catch {
          // Ignore forms that were deleted or aren't accessible
        }
      })
    );

    return results.sort((a, b) => b.id - a.id);
  },

  async update(id: number, dto: FormularioDTO): Promise<ResponseDTO> {
    return request<ResponseDTO>(`/Formulario/${id}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  async remove(id: number): Promise<ResponseDTO> {
    const res = await request<ResponseDTO>(`/Formulario/${id}`, {
      method: 'DELETE',
    });
    removeKnownFormId(id);
    return res;
  },
};

// ==================== PERGUNTA CONTROLLER (/api/Pergunta) ====================
export const perguntaApi = {
  async create(dto: PerguntaDTO): Promise<ResponseDTO<PerguntaResponseDTO>> {
    return request<ResponseDTO<PerguntaResponseDTO>>('/Pergunta', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  async getById(id: number): Promise<PerguntaResponseDTO> {
    return request<PerguntaResponseDTO>(`/Pergunta/${id}`, {
      method: 'GET',
    });
  },

  async getByFormulario(formularioId: number): Promise<PerguntaResponseDTO[]> {
    return request<PerguntaResponseDTO[]>(`/Pergunta/formulario/${formularioId}`, {
      method: 'GET',
    });
  },

  async update(id: number, dto: PerguntaDTO): Promise<ResponseDTO<PerguntaResponseDTO>> {
    return request<ResponseDTO<PerguntaResponseDTO>>(`/Pergunta/${id}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  async updateByFormulario(
    formularioId: number,
    perguntas: PerguntaDTO[]
  ): Promise<ResponseDTO<PerguntaResponseDTO[]>> {
    return request<ResponseDTO<PerguntaResponseDTO[]>>(
      `/Pergunta/formulario/${formularioId}`,
      {
        method: 'PUT',
        body: JSON.stringify(perguntas),
      }
    );
  },

  async remove(id: number): Promise<ResponseDTO> {
    return request<ResponseDTO>(`/Pergunta/${id}`, {
      method: 'DELETE',
    });
  },
};

// ==================== ALTERNATIVA CONTROLLER (/api/Alternativa) ====================
export const alternativaApi = {
  async create(dto: AlternativaDTO): Promise<ResponseDTO<AlternativaResponseDTO>> {
    return request<ResponseDTO<AlternativaResponseDTO>>('/Alternativa', {
      method: 'POST',
      body: JSON.stringify(dto),
    });
  },

  async getById(id: number): Promise<AlternativaResponseDTO> {
    return request<AlternativaResponseDTO>(`/Alternativa/${id}`, {
      method: 'GET',
    });
  },

  async getByPergunta(perguntaId: number): Promise<AlternativaResponseDTO[]> {
    return request<AlternativaResponseDTO[]>(`/Alternativa/pergunta/${perguntaId}`, {
      method: 'GET',
    });
  },

  async update(id: number, dto: AlternativaDTO): Promise<ResponseDTO<AlternativaResponseDTO>> {
    return request<ResponseDTO<AlternativaResponseDTO>>(`/Alternativa/${id}`, {
      method: 'PUT',
      body: JSON.stringify(dto),
    });
  },

  async updateByPergunta(
    perguntaId: number,
    alternativas: AlternativaDTO[]
  ): Promise<ResponseDTO<AlternativaResponseDTO[]>> {
    return request<ResponseDTO<AlternativaResponseDTO[]>>(
      `/Alternativa/pergunta/${perguntaId}`,
      {
        method: 'PUT',
        body: JSON.stringify(alternativas),
      }
    );
  },

  async remove(id: number): Promise<ResponseDTO> {
    return request<ResponseDTO>(`/Alternativa/${id}`, {
      method: 'DELETE',
    });
  },
};
