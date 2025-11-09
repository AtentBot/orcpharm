# 📊 Diagrama de Relacionamentos - Banco de Dados

## 🗂️ Estrutura Completa do Banco

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          CORE TABLES (Já Existentes)                     │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐       ┌──────────────────┐       ┌──────────────────┐
│  Establishment   │       │   AccessLevel    │       │  UserSession     │
├──────────────────┤       ├──────────────────┤       ├──────────────────┤
│ Id (PK)          │───┐   │ Id (PK)          │   ┌───│ Id (PK)          │
│ CategoryId (FK)  │   │   │ Code (UK)        │   │   │ EstablishmentId  │
│ RazaoSocial      │   │   │ Name             │   │   │ Token (UK)       │
│ CNPJ (UK)        │   │   │ CreatedAt        │   │   │ AccessLevel      │
│ AccessLevelId FK │───┼───│ UpdatedAt        │   │   │ ExpiresAt        │
│ ...              │   │   └──────────────────┘   │   └──────────────────┘
└──────────────────┘   │                          │
                       │                          │
┌─────────────────────────────────────────────────────────────────────────┐
│                          NEW CORE TABLES                                 │
└─────────────────────────────────────────────────────────────────────────┘
                       │                          │
                       │   ┌──────────────────┐  │
                       └───│ AccessProfile    │  │
                           ├──────────────────┤  │
                           │ Id (PK)          │  │
                           │ AccessLevelId FK │──┘
                           │ Code (UK)        │
                           │ HierarchyLevel   │
                           │ Permissions...   │
                           └──────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                          EMPLOYEE TABLES                                 │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐       ┌──────────────────┐
│   JobPosition    │◄──┐   │    Employee      │
├──────────────────┤   │   ├──────────────────┤
│ Id (PK)          │   └───│ Id (PK)          │
│ EstablishmentId  │───────│ EstablishmentId  │
│ Code (UK)        │       │ JobPositionId FK │───┐
│ Name             │       │ CPF (UK)         │   │
│ HierarchyLevel   │       │ FullName         │   │
│ ReportsTo (FK)   │───┐   │ Email            │   │
│ SalaryMin        │   │   │ Salary           │   │
│ SalaryMax        │   │   │ Status           │   │
│ ...              │   │   │ HireDate         │   │
└──────────────────┘   │   │ TerminationDate  │   │
         │             │   │ PasswordHash     │   │
         └─────────────┘   │ ...              │   │
                           └──────────────────┘   │
                                    │             │
            ┌───────────────────────┼─────────────┼───────────────────┐
            │                       │             │                   │
            ▼                       ▼             ▼                   ▼
┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐
│EmployeeJobHistory│   │ EmployeeSession  │   │EmployeeBenefit   │   │EmployeeDocument  │
├──────────────────┤   ├──────────────────┤   ├──────────────────┤   ├──────────────────┤
│ Id (PK)          │   │ Id (PK)          │   │ Id (PK)          │   │ Id (PK)          │
│ EmployeeId (FK)  │   │ EmployeeId (FK)  │   │ EmployeeId (FK)  │   │ EmployeeId (FK)  │
│ JobPositionId FK │   │ Token (UK)       │   │ BenefitType      │   │ DocumentType     │
│ StartDate        │   │ ExpiresAt        │   │ MonthlyValue     │   │ FilePath         │
│ EndDate          │   │ IsActive         │   │ ProviderName     │   │ FileHash         │
│ IsCurrent        │   │ IpAddress        │   │ StartDate        │   │ ExpiryDate       │
│ SalaryAtTime     │   │ UserAgent        │   │ EndDate          │   │ Status           │
│ ChangeReason     │   │ ...              │   │ IsActive         │   │ IsConfidential   │
│ ...              │   └──────────────────┘   │ ...              │   │ ...              │
└──────────────────┘                          └──────────────────┘   └──────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                          SECURITY TABLES                                 │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐       ┌──────────────────┐       ┌──────────────────┐
│   Permission     │       │ RolePermission   │       │   JobPosition    │
├──────────────────┤       ├──────────────────┤       ├──────────────────┤
│ Id (PK)          │◄──────│ Id (PK)          │───────│ Id (PK)          │
│ ResourceAction UK│       │ JobPositionId FK │       │ ...              │
│ Resource         │       │ PermissionId FK  │       └──────────────────┘
│ Action           │       │ EstablishmentId  │
│ Category         │       │ IsGranted        │
│ Scope            │       │ GrantedFrom      │
│ RiskLevel        │       │ GrantedUntil     │
│ RequiresApproval │       │ ...              │
│ ...              │       └──────────────────┘
└──────────────────┘
```

## 📋 Relacionamentos Detalhados

### 1. Establishment → Employee (1:N)
- Um estabelecimento possui vários funcionários
- `Employee.EstablishmentId` → `Establishment.Id`
- **Cascade**: Restrict (não permitir deletar establishment com funcionários)

### 2. JobPosition → Employee (1:N)
- Um cargo pode ter vários funcionários
- `Employee.JobPositionId` → `JobPosition.Id`
- **Cascade**: Restrict (proteger cargos em uso)

### 3. Employee → EmployeeJobHistory (1:N)
- Um funcionário tem múltiplos registros de histórico
- `EmployeeJobHistory.EmployeeId` → `Employee.Id`
- **Cascade**: Cascade (deletar histórico junto com funcionário)

### 4. Employee → EmployeeSession (1:N)
- Um funcionário pode ter múltiplas sessões ativas
- `EmployeeSession.EmployeeId` → `Employee.Id`
- **Cascade**: Cascade (revogar sessões ao deletar funcionário)

### 5. Employee → EmployeeBenefit (1:N)
- Um funcionário pode ter múltiplos benefícios
- `EmployeeBenefit.EmployeeId` → `Employee.Id`
- **Cascade**: Cascade

### 6. Employee → EmployeeDocument (1:N)
- Um funcionário pode ter múltiplos documentos
- `EmployeeDocument.EmployeeId` → `Employee.Id`
- **Cascade**: Cascade

### 7. JobPosition → RolePermission (1:N)
- Um cargo tem múltiplas permissões
- `RolePermission.JobPositionId` → `JobPosition.Id`
- **Cascade**: Cascade (remover permissões ao deletar cargo)

### 8. Permission → RolePermission (1:N)
- Uma permissão pode estar em múltiplos cargos
- `RolePermission.PermissionId` → `Permission.Id`
- **Cascade**: Cascade

### 9. JobPosition → JobPosition (Auto-Referência)
- Um cargo pode reportar a outro cargo (hierarquia)
- `JobPosition.ReportsTo` → `JobPosition.Id`
- **Cascade**: Restrict (proteger hierarquia)

## 🔍 Índices Criados

### Índices Únicos
- `employees.cpf` (UK)
- `employees.email` (UK)
- `job_positions.code` (UK)
- `employee_sessions.token` (UK)
- `permissions.resource_action` (UK)

### Índices de Busca
- `employees.establishment_id`
- `employees.job_position_id`
- `employees.status`
- `employee_job_history.employee_id`
- `employee_job_history.start_date`
- `employee_sessions.expires_at`
- `employee_sessions.is_active`
- `employee_benefits.benefit_type`
- `employee_documents.document_type`
- `employee_documents.expiry_date`
- `permissions.resource`
- `permissions.category`

## 📊 Estatísticas Estimadas

### Tamanho das Tabelas (Estimativa para 100 funcionários)

| Tabela | Registros | Tamanho Aprox. |
|--------|-----------|----------------|
| employees | 100 | ~500 KB |
| job_positions | 10 | ~10 KB |
| employee_job_history | 200 | ~50 KB |
| employee_sessions | 150 | ~100 KB |
| employee_benefits | 300 | ~75 KB |
| employee_documents | 500 | ~150 KB |
| permissions | 50 | ~15 KB |
| role_permissions | 200 | ~30 KB |
| **TOTAL** | **1510** | **~930 KB** |

## 🔐 Campos Sensíveis (LGPD)

Os seguintes campos devem ser criptografados:

### Employee
- ✅ `Cpf` (dados pessoais)
- ✅ `Rg` (dados pessoais)
- ✅ `PisPasep` (dados pessoais)
- ✅ `Salary` (dados financeiros)
- ✅ `BankAccount` (dados financeiros)

### Implementação Sugerida
```csharp
// Usar Data Protection API do .NET
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("OrcPharm");
```

## 📈 Performance

### Consultas Comuns Otimizadas

```sql
-- Buscar funcionários ativos com cargo e estabelecimento
SELECT e.*, jp.name as job_position, est.nome_fantasia
FROM employees e
INNER JOIN job_positions jp ON e.job_position_id = jp.id
INNER JOIN establishments est ON e.establishment_id = est.id
WHERE e.status = 'Ativo'
AND e.establishment_id = '{establishmentId}'
ORDER BY e.full_name;

-- Histórico de cargos de um funcionário
SELECT ejh.*, jp.name as position_name
FROM employee_job_history ejh
INNER JOIN job_positions jp ON ejh.job_position_id = jp.id
WHERE ejh.employee_id = '{employeeId}'
ORDER BY ejh.start_date DESC;

-- Verificar permissão
SELECT COUNT(*) > 0
FROM role_permissions rp
INNER JOIN permissions p ON rp.permission_id = p.id
INNER JOIN employees e ON e.job_position_id = rp.job_position_id
WHERE e.id = '{employeeId}'
AND p.resource_action = 'employees.create'
AND rp.is_active = true
AND rp.is_granted = true;
```

## 🎯 Próximas Expansões (Fases Futuras)

### Fase 2: Autenticação
- `password_reset_tokens`
- `two_factor_auth`

### Fase 3: Auditoria
- `audit_logs`
- `security_alerts`

### Fase 4: Ponto Eletrônico
- `time_entries`
- `schedules`
- `absences`

### Fase 5: Avaliações
- `performance_reviews`
- `goals`
- `feedback`

## 📞 Suporte

Para dúvidas sobre o modelo de dados:
1. Consulte este diagrama
2. Veja os exemplos em `EXEMPLOS.md`
3. Execute as queries de teste em `SETUP.md`

---

**Versão do Modelo**: 1.0.0  
**Data**: Novembro 2024  
**Compatível com**: PostgreSQL 12+, SQL Server 2019+
