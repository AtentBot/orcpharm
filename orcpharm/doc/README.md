# 🎯 OrcPharm - FASE 1: Estrutura de Dados e Modelos Base

## 📋 Visão Geral

Esta é a **Fase 1** do sistema de gestão de funcionários do OrcPharm. Nesta fase, criamos toda a estrutura de dados necessária para:

- ✅ Cadastro completo de funcionários (conforme CLT)
- ✅ Gestão de cargos e hierarquia
- ✅ Histórico de mudanças de cargo
- ✅ Sistema de sessões para funcionários
- ✅ Gestão de benefícios
- ✅ Armazenamento de documentos
- ✅ Sistema de permissões granulares
- ✅ Helpers para validações e cálculos trabalhistas

## 📁 Estrutura de Arquivos Criados

```
OrcPharm_Phase1/
├── Models/
│   ├── Employees/
│   │   ├── Employee.cs                    ⭐ Modelo principal de funcionário
│   │   ├── JobPosition.cs                 ⭐ Cargos e hierarquia
│   │   ├── EmployeeJobHistory.cs          ⭐ Histórico de mudanças
│   │   ├── EmployeeSession.cs             ⭐ Sessões de funcionários
│   │   ├── EmployeeBenefit.cs             ⭐ Benefícios (VT, VR, etc)
│   │   └── EmployeeDocument.cs            ⭐ Documentos (CTPS, ASO, etc)
│   ├── Core/
│   │   └── AccessProfile.cs               ⭐ Perfis de acesso expandidos
│   └── Security/
│       ├── Permission.cs                   ⭐ Permissões granulares
│       └── RolePermission.cs               ⭐ Associação cargo-permissão
├── DTOs/
│   └── Employees/
│       └── EmployeeRequests.cs            ⭐ DTOs para requisições
├── Helpers/
│   ├── DocumentValidator.cs               ⭐ Validação de CPF, CNPJ, PIS
│   ├── LaborLawCalculator.cs              ⭐ Cálculos CLT (férias, 13º, INSS, etc)
│   └── PasswordValidator.cs               ⭐ Validação e política de senhas
├── Data/
│   ├── AppDbContext.cs                    ⭐ Contexto atualizado
│   └── Configurations/
│       ├── EmployeeConfiguration.cs       ⭐ Configurações EF Core
│       ├── JobPositionConfiguration.cs
│       └── OtherConfigurations.cs
└── README.md                              📖 Este arquivo
```

## 🔧 Como Integrar no Projeto Existente

### 1️⃣ Copiar os Arquivos

Copie todos os arquivos criados para o seu projeto OrcPharm existente, mantendo a estrutura de pastas:

```bash
# Exemplo de estrutura final no seu projeto
OrcPharm/
├── Models/
│   ├── Employees/          # ✅ NOVO - Copiar desta pasta
│   ├── Core/               # ✅ NOVO - Copiar desta pasta
│   ├── Security/           # ✅ NOVO - Copiar desta pasta
│   ├── Establishment.cs    # Já existe
│   ├── AccessLevel.cs      # Já existe
│   └── ...
├── DTOs/                   # ✅ NOVO - Criar pasta e copiar
├── Helpers/                # ✅ NOVO - Criar pasta e copiar
├── Data/
│   ├── AppDbContext.cs     # ⚠️ SUBSTITUIR pelo novo
│   └── Configurations/     # ✅ NOVO - Copiar desta pasta
├── Controllers/
└── ...
```

### 2️⃣ Atualizar Dependências (se necessário)

Certifique-se de ter os pacotes NuGet necessários:

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Isopoh.Cryptography.Argon2
```

### 3️⃣ Criar a Migration

No diretório do projeto, execute:

```bash
# Criar a migration
dotnet ef migrations add AddEmployeeManagementSystem

# Aplicar ao banco de dados
dotnet ef database update
```

### 4️⃣ Verificar a Criação das Tabelas

As seguintes tabelas serão criadas:

| Tabela | Descrição |
|--------|-----------|
| `employees` | Dados completos dos funcionários |
| `job_positions` | Cargos e hierarquia |
| `employee_job_history` | Histórico de mudanças de cargo |
| `employee_sessions` | Sessões ativas de funcionários |
| `employee_benefits` | Benefícios (VT, VR, Plano de Saúde) |
| `employee_documents` | Documentos (CTPS, ASO, Certificados) |
| `permissions` | Permissões do sistema |
| `role_permissions` | Associação cargo ↔ permissão |
| `access_profiles` | Perfis de acesso expandidos |

## 📊 Dados Iniciais (Seed)

O sistema já vem com dados iniciais:

### Níveis de Acesso Padrão
- 👑 **Owner** (Proprietário) - Acesso total
- 👨‍💼 **Manager** (Gerente) - Gestão operacional
- 👷 **Employee** (Funcionário) - Operações básicas
- 👤 **User** (Usuário) - Visualização apenas

### Permissões Básicas
- `employees.create`, `employees.read`, `employees.update`, `employees.delete`, `employees.terminate`
- `inventory.read`, `inventory.update`
- `sales.create`, `sales.read`
- `reports.read`, `reports.export`
- `settings.update`

## 🧪 Testando os Helpers

### Validar CPF

```csharp
using Helpers;

bool isValid = DocumentValidator.IsValidCpf("12345678909");
string formatted = DocumentValidator.FormatCpf("12345678909");
// Resultado: "123.456.789-09"
```

### Validar Senha

```csharp
using Helpers;

var (isValid, errors) = PasswordValidator.ValidatePassword("MinhaSenh@123");
var (strength, level) = PasswordValidator.CalculatePasswordStrength("MinhaSenh@123");
```

### Cálculos CLT

```csharp
using Helpers;

// Calcular férias
decimal vacationPay = LaborLawCalculator.CalculateVacationPay(3000m, 12);

// Calcular 13º
decimal thirteenth = LaborLawCalculator.Calculate13thSalary(3000m, 12);

// Calcular INSS
var (inssValue, rate) = LaborLawCalculator.CalculateINSS(3000m);

// Calcular IRRF
var (irrfValue, effectiveRate) = LaborLawCalculator.CalculateIRRF(3000m, dependents: 2);

// Calcular salário líquido
decimal netSalary = LaborLawCalculator.CalculateNetSalary(3000m, dependents: 2);

// Calcular rescisão
var (totalValue, breakdown) = LaborLawCalculator.CalculateSeverancePay(
    salary: 3000m,
    hireDate: new DateOnly(2020, 1, 1),
    terminationDate: new DateOnly(2024, 12, 31)
);
```

## 📝 DTOs Disponíveis

### Criar Funcionário
```csharp
var request = new CreateEmployeeRequest
{
    EstablishmentId = Guid.Parse("..."),
    JobPositionId = Guid.Parse("..."),
    FullName = "João da Silva",
    Cpf = "12345678909",
    Email = "joao@example.com",
    HireDate = DateOnly.FromDateTime(DateTime.Now),
    Salary = 3000.00m,
    // ... outros campos
};
```

### Atualizar Funcionário
```csharp
var request = new UpdateEmployeeRequest
{
    Id = Guid.Parse("..."),
    Email = "novoemail@example.com",
    Salary = 3500.00m,
    // ... campos que deseja atualizar
};
```

### Demitir Funcionário
```csharp
var request = new TerminateEmployeeRequest
{
    EmployeeId = Guid.Parse("..."),
    TerminationDate = DateOnly.FromDateTime(DateTime.Now),
    TerminationType = "SemJustaCausa",
    TerminationReason = "Redução de quadro",
    RevokeAccessImmediately = true
};
```

## 🔐 Sistema de Permissões

### Como Funciona

1. **Permission**: Define uma ação sobre um recurso
   - Formato: `resource.action` (ex: `employees.create`)
   - Escopo: Own, Team, Establishment, Global
   - Nível de Risco: Low, Medium, High, Critical

2. **JobPosition**: Cargo do funcionário
   - Hierarquia de 1-10 (1 = baixo, 10 = alto)
   - Pode reportar a outro cargo

3. **RolePermission**: Associa cargo com permissão
   - Um cargo pode ter várias permissões
   - Permissões podem ser temporárias

### Exemplo de Uso

```csharp
// Verificar se funcionário tem permissão
bool hasPermission = await CheckPermissionAsync(employeeId, "employees.create");

// Atribuir permissão a um cargo
var rolePermission = new RolePermission
{
    JobPositionId = managerPositionId,
    PermissionId = createEmployeePermissionId,
    EstablishmentId = establishmentId,
    IsGranted = true,
    IsPermanent = true
};
```

## 🎯 Próximos Passos (Fase 2)

Na **Fase 2**, implementaremos:

1. ✅ Controllers para CRUD de funcionários
2. ✅ Sistema de autenticação de funcionários
3. ✅ Middleware de permissões
4. ✅ Endpoints de recuperação de senha
5. ✅ Two-Factor Authentication (2FA)

## ⚠️ Notas Importantes

### Conformidade CLT
- ✅ Todos os campos obrigatórios pela CLT estão presentes
- ✅ Cálculos de férias, 13º, INSS e IRRF implementados
- ✅ Controle de período de experiência
- ✅ Histórico de mudanças de cargo
- ✅ Gestão de benefícios

### Segurança
- ✅ Senhas com hash Argon2id
- ✅ Validação de CPF, CNPJ e PIS
- ✅ Política de senhas forte
- ✅ Sistema de permissões granulares
- ✅ Auditoria preparada (CreatedBy, UpdatedBy)

### LGPD
- ✅ Campo para nome social
- ✅ Campos sensíveis preparados para criptografia
- ✅ Soft delete preparado (DeletedBy, DeletedAt)
- ✅ Consentimento de dados preparado

## 🆘 Suporte

Se encontrar problemas:

1. Verifique se todas as dependências estão instaladas
2. Certifique-se que o PostgreSQL está rodando
3. Verifique a connection string no `appsettings.json`
4. Execute `dotnet ef database update` novamente

## 📚 Documentação Adicional

- **RDC 67/2007**: Boas práticas de manipulação
- **CLT**: Consolidação das Leis do Trabalho
- **LGPD**: Lei Geral de Proteção de Dados
- **Entity Framework Core**: https://docs.microsoft.com/ef/core/

---

**Versão**: 1.0.0  
**Data**: Novembro 2024  
**Status**: ✅ Completo - Pronto para Fase 2
