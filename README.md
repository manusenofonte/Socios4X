# Socios4X — Asistente Virtual con RAG

**🇦🇷 [Español](#español) | 🇺🇸 [English](#english)**

---

## Español

Asistente virtual que responde consultas de los socios de un club (altas, cuotas, pagos, credencial virtual, trámites) usando **RAG** (Retrieval-Augmented Generation) sobre la documentación real del sistema — no un chatbot genérico, responde solo con información verificable y admite explícitamente cuando no sabe algo.

Construido para el sistema de gestión de socios del **Club Atlético Independiente de Dolores**.

### 🔗 Demo en vivo

- **App**: [venerable-stroopwafel-f1617d.netlify.app](https://venerable-stroopwafel-f1617d.netlify.app)

> El backend corre en un plan gratuito que "duerme" sin tráfico — la primera carga puede tardar ~30-60s en despertar.

### 🧠 Cómo funciona

Al cargar un documento: se extrae el texto → se divide en fragmentos → cada fragmento se convierte en un vector con `text-embedding-3-small` → se guarda en Qdrant.

Al preguntar: la pregunta se convierte en vector con el mismo modelo → Qdrant devuelve los fragmentos más relevantes por similitud semántica (no coincidencia literal de palabras) → esos fragmentos se inyectan como contexto en el prompt → `gpt-5-nano` genera la respuesta usando *solo* ese contexto.

El modelo nunca ve los documentos completos, y tiene instrucción explícita de admitir cuando no encuentra la respuesta en vez de inventar.

### 🏗️ Stack

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=flat&logo=csharp&logoColor=white)
![Angular](https://img.shields.io/badge/Angular_21-DD0031?style=flat&logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=flat&logo=typescript&logoColor=white)
![OpenAI](https://img.shields.io/badge/OpenAI-412991?style=flat&logo=openai&logoColor=white)
![Qdrant](https://img.shields.io/badge/Qdrant-DC244C?style=flat&logo=qdrant&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat&logo=docker&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=flat&logo=microsoftsqlserver&logoColor=white)

- **Backend**: .NET 10, ASP.NET Core Web API, Clean Architecture (Domain / Application / Infrastructure / Api), Entity Framework Core, Microsoft Semantic Kernel
- **IA**: OpenAI (`gpt-5-nano` para chat, `text-embedding-3-small` para embeddings), Qdrant Cloud como base de datos vectorial
- **Frontend**: Angular 21 standalone components, TypeScript, render de Markdown sanitizado (marked + DOMPurify)
- **Infra**: Docker (backend en Render), Netlify (frontend), SQL Server

### ✨ Detalles técnicos

- **Clean Architecture real**: `Application` define las interfaces (`IVectorKnowledgeRepository`, `IFAQRepository`) sin conocer Qdrant ni SQL Server; `Infrastructure` las implementa. Cambiar de proveedor de vectores o de modelo es escribir una implementación nueva, sin tocar los casos de uso.
- **Seguridad**: rate limiting por IP, validación de inputs, sin fuga de detalles internos en errores 500, secrets fuera del código (variables de entorno / user-secrets, nunca en el repo).
- **Desplegado de verdad**: backend containerizado corriendo en Render, frontend en Netlify, base vectorial en Qdrant Cloud — tres servicios independientes con CORS configurado entre ellos.
- **Frontend cuidado**: tema claro/oscuro persistido, tipografía autohospedada (sin dependencias externas en runtime), accesibilidad básica, tests unitarios (Vitest).

### 📄 Documentación técnica / cómo correrlo local

Ver [`socios-frontend/README.md`](socios-frontend/README.md).

### Autoría

Desarrollado por **Nicolás Licha & Manuel Senofonte** — arquitectura, pipeline RAG, API, integración frontend y despliegue.

[GitHub](https://github.com/nicolaslicha02) · [LinkedIn](https://www.linkedin.com/in/nicolas-licha/)

---

## English

Virtual assistant that answers member questions for a club management system (memberships, dues, payments, virtual ID cards, procedures) using **RAG** (Retrieval-Augmented Generation) over the system's real documentation — not a generic chatbot, it only answers with verifiable information and explicitly says when it doesn't know something.

Built for **Club Atlético Independiente de Dolores**'s membership management system.

### 🔗 Live demo

- **App**: [venerable-stroopwafel-f1617d.netlify.app](https://venerable-stroopwafel-f1617d.netlify.app)

> The backend runs on a free tier that sleeps when idle — the first load after inactivity can take ~30-60s to spin back up.

### 🧠 How it works

On document upload: text is extracted → split into chunks → each chunk is embedded with `text-embedding-3-small` → stored in Qdrant.

On a question: the question is embedded with the same model → Qdrant returns the most relevant chunks by semantic similarity (not literal keyword matching) → those chunks are injected as context into the prompt → `gpt-5-nano` generates the answer using *only* that context.

The model never sees full documents, and is explicitly instructed to say it doesn't know rather than make something up.

### 🏗️ Stack

- **Backend**: .NET 10, ASP.NET Core Web API, Clean Architecture (Domain / Application / Infrastructure / Api), Entity Framework Core, Microsoft Semantic Kernel
- **AI**: OpenAI (`gpt-5-nano` for chat, `text-embedding-3-small` for embeddings), Qdrant Cloud as the vector database
- **Frontend**: Angular 21 standalone components, TypeScript, sanitized Markdown rendering (marked + DOMPurify)
- **Infra**: Docker (backend on Render), Netlify (frontend), SQL Server

### ✨ Technical highlights

- **Real Clean Architecture**: `Application` defines the interfaces (`IVectorKnowledgeRepository`, `IFAQRepository`) with no knowledge of Qdrant or SQL Server; `Infrastructure` implements them. Swapping the vector store or model provider means writing a new implementation, not touching the use cases.
- **Security**: per-IP rate limiting, input validation, no internal error details leaked on 500s, secrets kept out of the codebase (environment variables / user-secrets, never committed).
- **Actually deployed**: containerized backend running on Render, frontend on Netlify, vector store on Qdrant Cloud — three independent services with CORS configured between them.
- **Polished frontend**: persisted light/dark theme, self-hosted fonts (no external runtime dependencies), basic accessibility, unit tests (Vitest).

### 📄 Technical docs / running it locally

See [`socios-frontend/README.md`](socios-frontend/README.md).

### Authors

Built by **Nicolás Licha & Manuel Senofonte** — architecture, RAG pipeline, API, frontend integration, and deployment.

[GitHub](https://github.com/nicolaslicha02) · [LinkedIn](https://www.linkedin.com/in/nicolas-licha/)