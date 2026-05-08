# OrcPharm / Farmify

Plataforma SaaS completa para **farmácias de manipulação**, oferecendo desde o sistema de gestão (PDV, manipulação, fórmulas, controle fiscal, qualidade) até um marketplace público com app mobile para o paciente final.

> **Identidade visual:** Farmify v1.0 — paleta bone caloroso, brand SVG cápsula-F, tipografia General Sans + Cabinet Grotesk. Fonte da verdade dos tokens em [orcpharm/wwwroot/css/design-system/tokens.css](orcpharm/wwwroot/css/design-system/tokens.css).

---

## Workspaces

O monorepo agrupa três frentes que conversam pela mesma API:

| Pasta | Stack | Função |
|---|---|---|
| [orcpharm/](orcpharm/) | ASP.NET Core 9 + EF Core + PostgreSQL | Backend principal (gestão SaaS, marketplace, APIs mobile, painel admin, portal do cliente) |
| [orcpharm.Tests/](orcpharm.Tests/) | xUnit + Moq | Testes unitários (commission, JWT, mobile orders, workflow de pedidos) |
| [app/](app/) | React Native + Expo | App do paciente (FormulaCare) — login, receita por câmera, fórmula personalizada, pedidos |
| [Design_app/](Design_app/), [design/](design/) | HTML/JSX estáticos | Mockups e propostas do design system Farmify (handoff) |

---

## Stack do backend

- **Framework:** ASP.NET Core 9 (MVC + Web API + Razor Views)
- **Banco:** PostgreSQL via Npgsql + EF Core 9 (`EFCore.NamingConventions` em snake_case)
- **Autenticação:** Cookie (admin/employee/customer web) + JWT Bearer (`MobileJwt`, para `/api/mobile/`) + Session Token customizado
- **Hash de senha:** Argon2id (`Isopoh.Cryptography.Argon2`)
- **Pagamentos:** Stripe (`Stripe.net`), MercadoPago, Abacatepay (via `IPaymentGatewayFactory`)
- **PDF/QR:** QuestPDF, QRCoder
- **Validação:** FluentValidation
- **Notificações:** WhatsApp via AtentBot API + SMTP
- **OCR de receita:** OpenAI (`gpt-4o`)
- **Background jobs:** `IHostedService` — `TrialExpirationJob`, `SubscriptionMaintenanceJob`, `WeeklyCommissionJob`, `AbandonedCartCleanupJob`
- **Rate limiting:** `Microsoft.AspNetCore.RateLimiting` (políticas `auth`, `signup`, `resend-code`, `password-reset`) + `RateLimitMiddleware` para APIs mobile
- **Docs:** Swagger (apenas em Development) sob `/swagger`
- **Frontend (web):** Bootstrap 5 + design system Farmify customizado

---

## Estrutura do backend

```
orcpharm/
├── Controllers/              # 60+ controllers
│   ├── Mobile/               # APIs JWT para o app FormulaCare
│   ├── Pharmacy/             # Portal da farmácia no marketplace
│   ├── AdminMarketplaceController.cs
│   ├── StripeWebhookController.cs
│   ├── SubscriptionPortalController.cs
│   └── ...
├── Service/                  # Camada de negócio
│   ├── Auth/                 # AuthService, password reset
│   ├── Marketplace/          # CommissionService, JwtTokenService, OrderNotificationService
│   ├── Jobs/                 # Background jobs
│   ├── Notifications/        # WhatsApp
│   ├── Prescriptions/        # OCR de receita, quote workflow
│   ├── CustomerFormulas/     # RefundService, CustomFormulaService
│   ├── Sale/                 # PaymentService
│   └── ...
├── Middleware/               # AdminAuth, EmployeeAuth, CustomerAuth,
│                             # JwtAuthMiddleware, RateLimitMiddleware,
│                             # SubscriptionLimit, SubscriptionRequired
├── Models/
│   ├── Marketplace/          # Address, Device, Rating, Commission, Transaction
│   ├── Security/             # AuditLog
│   └── ...
├── Migrations/               # EF Core migrations (PostgreSQL)
├── Views/                    # Razor (.cshtml) — 216 arquivos
├── DTOs/
│   ├── Mobile/               # DTOs do app
│   └── Pharmacy/             # DTOs do portal de farmácia
├── ViewModels/
└── wwwroot/
    ├── css/design-system/    # Tokens Farmify (fonte da verdade)
    ├── js/                   # signup, pdv, payment-management, prescription-ocr, landing, ...
    └── images/
```

---

## Pré-requisitos

- .NET SDK **9.0**
- PostgreSQL **14+** (local, Docker, ou gerenciado)
- Node.js **18+** + npm (apenas para o workspace [app/](app/))
- (Opcional) Docker Desktop — modo recomendado para subir o Postgres em dev

---

## Configuração

### 1. Backend — `appsettings.json`

O arquivo real **não é versionado** (`.gitignore`). Copie o template:

```powershell
Copy-Item orcpharm/appsettings.Example.json orcpharm/appsettings.json
Copy-Item orcpharm/appsettings.Example.json orcpharm/appsettings.Development.json
```

Edite com seus valores. Chaves obrigatórias:

| Chave | Para quê |
|---|---|
| `ConnectionStrings:DefaultConnection` | Postgres |
| `OpenAI:ApiKey` | OCR de receita (`gpt-4o`) |
| `Encryption:Key` | AES base64 — usado pelo `AesEncryptionService` |
| `AtentBot:ApiKey` | Envio de WhatsApp |
| `Cloudflare:Turnstile*` | Captcha do signup |
| `EmailSettings:*` | SMTP transacional |
| `Cors:AllowedOrigins` | Domínios autorizados em produção |
| `SeedAdmin:Email/Password` | Primeiro `SaasAdmin` (criado no boot se a tabela estiver vazia) |
| `Jwt:Key/Issuer/Audience` | Tokens das APIs mobile |
| `ApiKeyAuth:Keys` | API keys com hash base64 |

> Em produção, prefira injetar via env var: `ConnectionStrings__DefaultConnection`, `Stripe__ApiKey`, etc.

### 2. Mobile — [app/src/constants/config.js](app/src/constants/config.js)

Defina a URL da API:

```js
export const API_URL = 'https://orcpharm.atentbot.com/api';
```

---

## Rodando localmente

### Backend

```powershell
# Da raiz do repo:
dotnet restore orcpharm/orcpharm.sln
dotnet run --project orcpharm/orcpharm.csproj
```

No primeiro boot, o app:
1. Em **Development**: roda `EnsureCreatedAsync()` (cria as tabelas a partir do modelo atual).
2. Em **Production**: roda `MigrateAsync()` (aplica migrations pendentes).
3. Faz seed do `AccessLevel "FARM"` e do primeiro `SaasAdmin` se ainda não existirem.

Endpoints úteis em dev:
- `https://localhost:5001/` — landing pública
- `https://localhost:5001/swagger` — docs OpenAPI (apenas dev)
- `https://localhost:5001/health` — health check
- `https://localhost:5001/admin/login` — painel SaaS

### Mobile (Expo)

```powershell
cd app
npm install
npx expo start
# escanear o QR code com o Expo Go
```

### Tests

```powershell
dotnet test orcpharm.Tests/orcpharm.Tests.csproj
```

---

## Migrations

```powershell
# Criar migration
dotnet ef migrations add NomeDaMigration --project orcpharm/orcpharm.csproj

# Aplicar
dotnet ef database update --project orcpharm/orcpharm.csproj

# Reverter para uma migration específica
dotnet ef database update NomeAnterior --project orcpharm/orcpharm.csproj
```

> O `MigrationsAssembly` está fixado em `orcpharm`. A última migration aplicada é `20260313174820_AddMarketplaceTables`.

---

## Camadas de auth (resumo)

A pipeline em [Program.cs](orcpharm/Program.cs#L444-L451) encadeia, nesta ordem:

```
Authentication → JwtAuthMiddleware → AdminAuth → EmployeeAuth →
CustomerAuth → SubscriptionLimits → SubscriptionRequired → Authorization
```

| Middleware | Escopo |
|---|---|
| `JwtAuthMiddleware` | Rotas `/api/mobile/*` (token JWT do `JwtTokenService`) |
| `AdminAuth` | Painel SaaS (`SaasAdmin`) |
| `EmployeeAuth` | Funcionários da farmácia |
| `CustomerAuth` | Portal/cart do paciente |
| `SubscriptionLimits` | Aplica limites do plano contratado |
| `SubscriptionRequired` | Bloqueia farmácias com subscription inválida |

---

## Marketplace e mobile

**Backend (`/api/mobile/*`):** auth via JWT, controllers em [orcpharm/Controllers/Mobile/](orcpharm/Controllers/Mobile/) — auth, addresses, cart, orders, pharmacies, ratings, search.

**Portal da farmácia (`Pharmacy/*`):** controllers em [orcpharm/Controllers/Pharmacy/](orcpharm/Controllers/Pharmacy/) — catalog, financial, orders, marketplace settings.

**Comissionamento:** `CommissionService` + job semanal `WeeklyCommissionJob` calcula a comissão da plataforma sobre cada pedido.

**Documentação detalhada:** [orcpharm/MARKETPLACE_PROJECT_GUIDE.md](orcpharm/MARKETPLACE_PROJECT_GUIDE.md).

---

## Status / pontos abertos

Um checklist completo de pré-produção vive em `CHECKLIST_PRE_PRODUCAO.md` (não versionado). Itens conhecidos a tratar antes de subir:

- CORS aberto em desenvolvimento — restringir explicitamente em produção
- `RefundService` ainda é mock (não estorna no Stripe)
- `StripeConfiguration.ApiKey` setado globalmente — risco de race condition
- Há dois pontos de webhook do Stripe ([StripeController](orcpharm/Controllers/StripeController.cs) e [StripeWebhookController](orcpharm/Controllers/StripeWebhookController.cs)) — falta unificar
- Swagger fica disponível apenas em Development (correto), mas validar build de produção

---

## Convenções

- Nomes de tabelas/colunas em **snake_case** via `EFCore.NamingConventions`.
- Migrations sempre testadas com `EnsureCreated` (dev) e `Migrate` (prod) — não usar `Database.EnsureCreated` em produção.
- Senhas com **Argon2id** (parâmetros default da lib `Isopoh`).
- Tokens JWT mobile: `Issuer`/`Audience` em `appsettings`, validade configurável no `JwtTokenService`.
- Templates de e-mail em `Service/EmailService.cs` + parciais.
- Tokens visuais centralizados em `wwwroot/css/design-system/tokens.css` — não inserir hex diretamente em CSS/HTML.

---

## Licença

Projeto proprietário — © OrcPharm / Farmify.
