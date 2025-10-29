# FSI.ChatProxy

Um **proxy de chat** em .NET que fica entre o cliente e provedores de IA/LLMs, padronizando autenticação (JWT), roteamento de requisições e políticas de segurança (rate limit, blacklist de tokens, limpeza de segredos).

> **Status**: Em desenvolvimento. Estrutura confirmada: `src/` com o projeto `ChatProxy.Api`.

---

## ✨ Principais recursos

- **Proxy HTTP para Chat/LLM**: endpoint unificado para encaminhar prompts e respostas.
- **Autenticação JWT (Bearer)**: proteção das rotas com validação de token.
- **Blacklist de tokens**: possibilidade de invalidar JWT antes do vencimento.
- **Boas práticas de segredos**: variáveis de ambiente/`dotnet user-secrets` (não commitar API keys).
- **Observabilidade**: logs estruturados; pontos de extensão para métricas.

> Dica: ative Push Protection/Secret Scanning no GitHub e **nunca** commite chaves em `appsettings.json`.

---

## 🧱 Arquitetura (visão geral)

```
FSI.ChatProxy/
├─ src/
│  └─ ChatProxy.Api/    # ASP.NET (minimal API/Web API) - host do proxy
├─ .gitignore
└─ README.md
```

---

## 🚀 Começando

### Pré-requisitos

- .NET SDK 8.0+
- (Opcional) Redis/SQLite/SQL Server – para blacklist persistente de JWT
- Chave(s) dos provedores (ex.: OpenAI/Ollama/Anthropic) **via variável de ambiente**

### Clonar

```bash
git clone https://github.com/rodrigofurlaneti/FSI.ChatProxy.git
cd FSI.ChatProxy
```

### Variáveis de ambiente (exemplos)

Use **um** dos métodos abaixo:

#### 1) `dotnet user-secrets` (recomendado para Dev)

```bash
cd src/ChatProxy.Api
dotnet user-secrets init

dotnet user-secrets set "Jwt:Issuer" "FSI.ChatProxy"
dotnet user-secrets set "Jwt:Audience" "FSI.ChatProxy.Clients"
dotnet user-secrets set "Jwt:Key" "chave-super-secreta-256bits"

dotnet user-secrets set "OpenAI:ApiKey" "sk-***"
dotnet user-secrets set "OpenAI:BaseUrl" "https://api.openai.com/v1"
```

#### 2) Variáveis de ambiente (Windows PowerShell)

```powershell
$env:Jwt__Issuer="FSI.ChatProxy"
$env:Jwt__Audience="FSI.ChatProxy.Clients"
$env:Jwt__Key="chave-super-secreta-256bits"

$env:OpenAI__ApiKey="sk-***"
$env:OpenAI__BaseUrl="https://api.openai.com/v1"
```

> **Não** coloque chaves em `appsettings.json`.

### Rodando a API

```bash
dotnet restore
dotnet build
dotnet run --project src/ChatProxy.Api
```

Por padrão, a API fica disponível em `https://localhost:7244`.

---

## 🔐 Autenticação e segurança

- **JWT Bearer** em cabeçalho `Authorization: Bearer <token>`.
- **Blacklist de tokens**:
  - Estratégia simples (em memória) para desenvolvimento.
  - Em produção, prefira backend persistente (Redis/DB) com TTL do token.

### Geração de JWT (exemplo de payload)

```json
{
  "sub": "admin",
  "name": "admin",
  "roles": ["admin"],
  "iss": "FSI.ChatProxy",
  "aud": "FSI.ChatProxy.Clients",
  "nbf": 1761767333,
  "exp": 1761767633
}
```

---

## 📡 Endpoints

### POST `/chat/ask`

- **Headers**
  - `Authorization: Bearer <seu_jwt>`
  - `Content-Type: application/json`

- **Body (exemplo)**

```json
{
  "prompt": "Explique CQRS em 3 tópicos."
}
```

- **cURL**

```bash
curl -X POST "https://localhost:7244/chat/ask" ^
  -H "accept: application/json" ^
  -H "Authorization: Bearer <seu_token_jwt>" ^
  -H "Content-Type: application/json" ^
  -d "{"prompt": "Seu prompt aqui"}"
```

- **Resposta (exemplo)**

```json
{
  "message": "conteúdo retornado pelo provedor",
  "model": "gpt-4o-mini",
  "tokens": { "input": 10, "output": 120 }
}
```

---

## ⚙️ Configuração

```jsonc
{
  "Jwt": {
    "Issuer": "FSI.ChatProxy",
    "Audience": "FSI.ChatProxy.Clients",
    "Key": "NUNCA-commitar-esta-chave"
  },
  "Providers": {
    "OpenAI": {
      "BaseUrl": "https://api.openai.com/v1",
      "ApiKey": "<use-variavel-de-ambiente>"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 🧪 Testes

- Recomenda-se xUnit + FluentAssertions.
- Testes devem validar autenticação, blacklist e respostas de erro do provedor.

---

## 🐳 Docker (opcional)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ./src/ChatProxy.Api ./ChatProxy.Api
RUN dotnet publish ChatProxy.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ChatProxy.Api.dll"]
```

Rodar:

```bash
docker build -t fsi-chatproxy .
docker run -p 8080:8080 ^
  -e Jwt__Issuer=FSI.ChatProxy ^
  -e Jwt__Audience=FSI.ChatProxy.Clients ^
  -e Jwt__Key="chave-256bits" ^
  -e OpenAI__ApiKey=sk-*** ^
  fsi-chatproxy
```

---

## 📚 Roadmap

- [ ] Documentar endpoints com Swagger
- [ ] Rate limiting configurável
- [ ] Providers plugáveis (OpenAI, Ollama, Azure OpenAI)
- [ ] Persistência para blacklist (Redis/DB)
- [ ] Métricas (Prometheus/OpenTelemetry)

---

## 🤝 Contribuindo

1. Crie uma branch: `feat/minha-melhoria`
2. Commits pequenos e descritivos
3. Abra a PR explicando o que foi alterado

---

## 📄 Licença

MIT License

---

## 🧭 Contato

**Autor:** Rodrigo Luiz Madeira Furlaneti  
**Repositório:** [FSI.ChatProxy](https://github.com/rodrigofurlaneti/FSI.ChatProxy)
