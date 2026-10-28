export type PerfilUsuario = 'Administrador' | 'Comum';

export type StatusFormulario = 'Rascunho' | 'Publicado' | 'Encerrado';

export type TipoPergunta =
  | 'RespostaCurta'
  | 'TextoLongo'
  | 'MultiplaEscolha'
  | 'CaixaDeSelecao';

export interface ResponseDTO<T = undefined> {
  status: boolean;
  message: string;
  data?: T;
}

export interface LoginDTO {
  email: string;
  senha: string;
}

export interface UsuarioDTO {
  nome: string;
  email: string;
  senha: string;
  perfil?: PerfilUsuario;
}

export interface UsuarioModel {
  id: number;
  nome: string;
  email: string;
  senha?: string;
  perfil: PerfilUsuario;
}

export interface AlternativaDTO {
  id?: number;
  texto: string;
  perguntaId?: number;
}

export interface AlternativaResponseDTO {
  id: number;
  texto: string;
  perguntaId: number;
}

export interface PerguntaDTO {
  id?: number;
  enunciado: string;
  tipo: TipoPergunta;
  ordem: number;
  obrigatoria: boolean;
  formularioId?: number;
  alternativas?: AlternativaDTO[];
}

export interface PerguntaResponseDTO {
  id: number;
  enunciado: string;
  tipo: TipoPergunta;
  ordem: number;
  obrigatoria: boolean;
  formularioId: number;
  alternativas: AlternativaResponseDTO[];
}

export interface FormularioDTO {
  titulo: string;
  descricao?: string;
  status: StatusFormulario;
  perguntas?: PerguntaDTO[];
}

export interface FormularioResponseDTO {
  id: number;
  titulo: string;
  descricao?: string;
  dataCriacao: string;
  dataAtualizacao?: string;
  status: StatusFormulario;
  perguntas: PerguntaResponseDTO[];
}

export interface AuthSession {
  token: string;
  userId: number;
  email: string;
  perfil: PerfilUsuario;
  nome?: string;
}

export interface FormSubmissionRecord {
  id: string;
  formularioId: number;
  usuarioEmail: string;
  dataEnvio: string;
  respostas: Record<number, string | string[]>;
}
