# 📝 Forms - Plataforma Completa de Gestão de Formulários

<div align="center">

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-14-239120?logo=c-sharp&logoColor=white)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-5.x%20%2F%206.x-3178C6?logo=typescript&logoColor=white)
![Vite](https://img.shields.io/badge/Vite-8.x-646CFF?logo=vite&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC292B?logo=microsoft-sql-server&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-xUnit%20%7C%20Moq-00B4D8?logo=xunit&logoColor=white)

Uma plataforma **full-stack moderna e escalável** para criação, personalização, compartilhamento e preenchimento de formulários dinâmicos — inspirada em ferramentas como Google Forms e Typeform.

[Recursos](#-recursos-principais) •
[Arquitetura](#-arquitetura-da-solução) •
[Tecnologias](#-tecnologias-utilizadas) •
[Como Rodar](#-como-executar-o-projeto) •
[Variáveis de Ambiente](#-variáveis-de-ambiente) •
[Endpoints da API](#-documentação-da-api-endpoints) •
[Testes](#-testes-automatizados)

</div>

---

## 🎯 Sobre o Projeto

O **Forms** é um ecossistema completo composto por uma **Web API robusta em ASP.NET Core 10**, uma **SPA moderna em React 19 com Vite e TypeScript**, persistência em **SQL Server com Entity Framework Core 10** e orquestração total via **Docker Compose**.

A aplicação permite que usuários criem formulários complexos com perguntas de diversos formatos, configurem alternativas, publiquem seus questionários através de links únicos e coletem respostas em tempo real com controle de acesso e auditoria.

---

## ✨ Recursos Principais

### 📋 Construtor e Gestão de Formulários
- **Criação e Edição Intuitiva:** Criação de formulários personalizados com título, descrição e ciclo de vida (`Rascunho`, `Publicado`, `Encerrado`).
- **Múltiplos Tipos de Perguntas:**
  - 📝 **Resposta Curta:** Textos breves de linha única.
  - 📄 **Texto Longo:** Campos de texto amplos (área de texto).
  - 🔘 **Múltipla Escolha:** Seleção exclusiva de uma opção entre alternativas.
  - ☑️ **Caixa de Seleção:** Seleção de múltiplas opções simultâneas.
- **Perguntas Obrigatórias:** Controle para marcar questões como de preenchimento obrigatório.
- **Alternativas Dinâmicas:** Adição, edição e remoção de opções em lote ou individualmente.
- **Compartilhamento Simplificado:** Geração de link direto para resposta pública/privada.

### ✍️ Preenchimento e Respostas (Responder)
- **Área Exclusiva de Preenchimento:** Interface limpa focada na experiência do respondente.
- **Validação em Tempo Real:** Verificação de campos obrigatórios e formato de respostas antes do envio.
- **Controle de Status:** Bloqueio automático de envios para formulários encerrados ou em modo rascunho.

### 🔐 Segurança, Usuários e Perfis
- **Autenticação JWT:** Proteção de rotas através de JSON Web Tokens com Bearer Authentication.
- **Criptografia Forte:** Senhas armazenadas com hash seguro via **BCrypt**.
- **Controle de Acesso Baseado em Perfis (RBAC):**
  - **Administrador:** Visualização e gestão completa de todos os usuários e formulários do sistema.
  - **Comum:** Gestão e acompanhamento apenas dos seus próprios formulários e respostas.
- **Tratamento Centralizado de Exceções:** Middleware customizado que garante respostas padronizadas e sem vazamento de dados sensíveis.

---

## 🏗️ Arquitetura da Solução

O projeto segue boas práticas de engenharia de software e Clean Architecture simplificada, separando responsabilidades em camadas bem definidas:

```
Forms/
├── src/
│   ├── Forms.API/             # Backend
│   │   ├── Controllers/       # Endpoints HTTP
│   │   ├── Services/          # Regras de negócio e interfaces
│   │   ├── Repositories/      # Acesso a dados com EF Core
│   │   ├── Models/            # Entidades do banco de dados
│   │   ├── DTOs/              # Objetos de transferência de dados 
│   │   ├── Enums/             # Enums de domínio
│   │   ├── Data/              # DbContext e mapeamento relacional
│   │   ├── Middlewares/       # Middleware global de captura de erros
│   │   ├── Migrations/        # Histórico de migrações do banco de dados
│   │   └── Dockerfile         # Imagem Docker
│   │
│   ├── Forms.API.Tests/       # Testes unitários com xUnit e Moq
│   │   ├── Controllers/       # Testes de integração/unidade de Controllers
│   │   ├── Services/          # Testes de unidade das regras de negócio
│   │   ├── Middlewares/       # Testes de interceptação de erros
│   │   └── Mappers/           # Testes de conversão e DTOs
│   │
│   └── Forms.WEB/             # Frontend
│       ├── src/
│       │   ├── components/    # Componentes de UI
│       │   ├── services/      # Cliente HTTP e integração com a API (/api)
│       │   ├── types/         # Tipagens TypeScript compartilhadas
│       │   └── assets/        # Estilos, ícones e recursos visuais
│       ├── nginx.conf         # Configuração de proxy e SPA para produção
│       └── Dockerfile         # Build estático com Node e deploy em Nginx
│
├── docker-compose.yml         # Orquestração do ambiente de produção/geral
├── docker-compose.dev.yml     # Orquestração de desenvolvimento com hot-reload (dotnet watch + vite)
├── .env.exemple               # Template das variáveis de ambiente necessárias
└── Forms.sln                  # Arquivo da Solução .NET
```

---

## 🛠️ Tecnologias Utilizadas

### Backend
- **Plataforma:** [.NET 10](https://dotnet.microsoft.com/) / **C# 14**
- **Framework:** ASP.NET Core Web API
- **ORM & Banco:** [Entity Framework Core 10](https://learn.microsoft.com/ef/core/) com [Microsoft SQL Server 2022](https://www.microsoft.com/sql-server)
- **Segurança & Criptografia:** `BCrypt.Net-Next` e `Microsoft.AspNetCore.Authentication.JwtBearer`
- **Configuração:** `DotNetEnv` para carregamento dinâmico de variáveis de ambiente
- **Documentação:** OpenAPI / Swagger (`Swashbuckle.AspNetCore`)

### Frontend
- **Biblioteca:** [React 19](https://react.dev/)
- **Linguagem:** [TypeScript](https://www.typescriptlang.org/)
- **Build Tool:** [Vite 8](https://vitejs.dev/)
- **Servidor Web:** [Nginx](https://www.nginx.com/) (em contêiner de produção)
- **Arquitetura de Proxy:** Proxy inteligente no Vite (`smart-api-proxy`) para direcionamento automático de chamadas `/api`

### Testes
- **Framework:** [xUnit](https://xunit.net/)
- **Mocks:** [Moq](https://github.com/moq/moq)

### DevOps & Contêineres
- **Docker** e **Docker Compose**
- Imagens otimizadas com *multi-stage builds*

---

## 🚀 Como Executar o Projeto

Você pode rodar a aplicação inteira de forma prática usando o **Docker Compose** ou executando os projetos **localmente**.

### Pré-requisitos
- [Docker](https://www.docker.com/) e [Docker Compose](https://docs.docker.com/compose/) *(para execução em contêineres)*
- **OU**, para execução local:
  - [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
  - [Node.js 22+](https://nodejs.org/)
  - [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (ou LocalDB)

---

### Executando o projeto 

#### 1. Clone o repositório
```bash
git clone https://github.com/Pedrouro/Forms.git
cd Forms
```

#### 2. Configure o arquivo `.env`
Copie o modelo de variáveis de ambiente:
```bash
cp .env.exemple .env
```
Edite o arquivo `.env` preenchendo as chaves desejadas (exemplo de configuração para o Docker Compose):
```env
DB_SENHA=SuaSenhaForte123!
ConnectionStrings__Default=Server=sqlserver,1433;Database=ProjetoFormsDb;User Id=sa;Password=SuaSenhaForte123!;TrustServerCertificate=True;
JWT_KEY=UmaChaveSecretaSuperSeguraComPeloMenos256Bits1234567890
JWT_ISSUER=ProjetoFormsAPI
JWT_AUDIENCE=ProjetoFormsClient
```

#### 3. Inicie os contêineres

**Modo Produção / Padrão:**
```bash
docker compose up --build
```

#### 4. Acessar a aplicação
- 🌐 **Frontend Web:** [http://localhost:3000](http://localhost:3000)
- ⚙️ **API REST / Swagger UI:** [http://localhost:5000/swagger](http://localhost:5000/swagger)
- 🗄️ **SQL Server:** `localhost:1433` (Usuário: `sa`)

---



## 🔑 Variáveis de Ambiente

O projeto utiliza um arquivo `.env` na raiz para parametrizar conexões e segredos:

| Variável | Descrição | Exemplo |
|---|---|---|
| `DB_SENHA` | Senha do usuário `sa` no container do SQL Server | `SuaSenhaForte123!` |
| `ConnectionStrings__Default` | String de conexão com o banco de dados | `Server=sqlserver;Database=ProjetoFormsDb;...` |
| `JWT_KEY` | Chave simétrica privada para assinatura de tokens JWT (mínimo 256 bits) | `ChaveSecreta123456789!` |
| `JWT_ISSUER` | Emissor do token JWT | `FormsAPI` |
| `JWT_AUDIENCE` | Audiência/Destinatário do token JWT | `FormsClient` |
| `VITE_API_TARGET` | *(Opcional)* URL de destino do proxy do Vite para a API | `http://api:8080` ou `http://localhost:5000` |

---

## 📖 Documentação da API (Endpoints)

Quando a API estiver em execução, acesse o **Swagger UI interativo** em:
👉 `http://localhost:5000/swagger`

### Autenticação (`/api/Auth`)
| Método | Endpoint | Descrição | Autenticação |
|:---:|---|---|:---:|
| `POST` | `/api/Auth` | Realiza login e gera o token Bearer JWT | Não |

### Usuários (`/api/Usuario`)
| Método | Endpoint | Descrição | Autenticação |
|:---:|---|---|:---:|
| `POST` | `/api/Usuario` | Cadastro de novos usuários comuns | Não |
| `GET` | `/api/Usuario/{id}` | Busca perfil do usuário por ID | Sim (`UserOnly`) |
| `GET` | `/api/Usuario` | Lista todos os usuários cadastrados | Sim (`AdminOnly`) |
| `PUT` | `/api/Usuario/{id}` | Atualiza dados cadastrais | Sim (`UserOnly`) |
| `DELETE` | `/api/Usuario/{id}` | Exclui usuário do sistema | Sim (`AdminOnly`) |

### Formulários (`/api/Formulario`)
| Método | Endpoint | Descrição | Autenticação |
|:---:|---|---|:---:|
| `POST` | `/api/Formulario` | Cria um novo formulário | Sim (`UserOnly`) |
| `GET` | `/api/Formulario/{id}` | Obtém detalhes completos do formulário | Sim (`UserOnly`) |
| `GET` | `/api/Formulario` | Lista todos os formulários | Sim (`AdminOnly`) |
| `PUT` | `/api/Formulario/{id}` | Atualiza dados e status do formulário | Sim (`UserOnly`) |
| `DELETE` | `/api/Formulario/{id}` | Remove formulário | Sim (`UserOnly`) |

### Perguntas (`/api/Pergunta`)
| Método | Endpoint | Descrição | Autenticação |
|:---:|---|---|:---:|
| `POST` | `/api/Pergunta` | Adiciona uma pergunta a um formulário | Sim (`UserOnly`) |
| `GET` | `/api/Pergunta/{id}` | Busca uma pergunta por ID | Sim (`UserOnly`) |
| `GET` | `/api/Pergunta/formulario/{formularioId}` | Lista todas as perguntas de um formulário | Sim (`UserOnly`) |
| `PUT` | `/api/Pergunta/{id}` | Atualiza uma pergunta individual | Sim (`UserOnly`) |
| `PUT` | `/api/Pergunta/formulario/{formularioId}` | Atualiza perguntas em lote para o formulário | Sim (`UserOnly`) |
| `DELETE` | `/api/Pergunta/{id}` | Remove uma pergunta | Sim (`UserOnly`) |

### Alternativas (`/api/Alternativa`)
| Método | Endpoint | Descrição | Autenticação |
|:---:|---|---|:---:|
| `POST` | `/api/Alternativa` | Cria alternativa para uma pergunta de seleção | Sim (`UserOnly`) |
| `GET` | `/api/Alternativa/{id}` | Obtém dados de uma alternativa | Sim (`UserOnly`) |
| `GET` | `/api/Alternativa/pergunta/{perguntaId}` | Lista alternativas de uma pergunta | Sim (`UserOnly`) |
| `PUT` | `/api/Alternativa/{id}` | Atualiza uma alternativa | Sim (`UserOnly`) |
| `PUT` | `/api/Alternativa/pergunta/{perguntaId}` | Atualiza alternativas em lote | Sim (`UserOnly`) |
| `DELETE` | `/api/Alternativa/{id}` | Exclui uma alternativa | Sim (`UserOnly`) |

---

## 🧪 Testes Automatizados

O projeto conta com suíte de testes unitários cobrindo regras de negócio, serviços, controle de exceções e mapeamento na camada `Forms.API.Tests`.

Para rodar todos os testes automatizados da solução:

```bash
dotnet test
```