using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Isopoh.Cryptography.Argon2;
using Helpers;
using Microsoft.AspNetCore.Authorization;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(AppDbContext db, ILogger<EmployeesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== LOGIN DO FUNCIONÁRIO ====================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        // Remover formatação do CPF
        var cpf = DocumentValidator.RemoveFormatting(dto.Cpf);

        // Validar CPF
        if (!DocumentValidator.IsValidCpf(cpf))

            return BadRequest(new { error = "CPF inválido" });

        try
        {
            // Buscar funcionário
            var employee = await _db.Employees
                .Include(e => e.Establishment)
                .Include(e => e.JobPosition)
                .FirstOrDefaultAsync(e => e.Cpf == cpf);
       

        if (employee == null)
            return Unauthorized(new { error = "Credenciais inválidas" });

        // Verificar status do funcionário
        if (employee.Status != "Ativo")
            return Unauthorized(new { error = $"Funcionário {employee.Status.ToLower()}" });

        // Verificar se está bloqueado
        if (employee.LockedUntil.HasValue && employee.LockedUntil.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (employee.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
            return Unauthorized(new { error = $"Conta bloqueada. Tente novamente em {Math.Ceiling(remainingMinutes)} minutos" });
        }

        // Verificar senha
        if (!Argon2.Verify(employee.PasswordHash, dto.Password))
        {
            // Incrementar tentativas falhas
            employee.FailedLoginAttempts++;

            if (employee.FailedLoginAttempts >= 5)
            {
                employee.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                await _db.SaveChangesAsync();
                return Unauthorized(new { error = "Conta bloqueada por 30 minutos devido a múltiplas tentativas falhas" });
            }

            await _db.SaveChangesAsync();
            return Unauthorized(new { error = "Credenciais inválidas" });
        }

        // Verificar se estabelecimento está ativo
        if (!employee.Establishment!.IsActive)
            return Unauthorized(new { error = "Estabelecimento inativo" });

        // Resetar tentativas falhas
        employee.FailedLoginAttempts = 0;
        employee.LockedUntil = null;

        // Criar sessão
        var session = new EmployeeSession
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers["User-Agent"].ToString(),
            DeviceType = GetDeviceType(Request.Headers["User-Agent"].ToString()),
            Browser = GetBrowser(Request.Headers["User-Agent"].ToString()),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(8),
            LastActivityAt = DateTime.UtcNow,
            IsActive = true,
            RequiresTwoFactor = employee.TwoFactorEnabled,
            TwoFactorVerified = !employee.TwoFactorEnabled, // Se não tem 2FA, já está verificado
            SessionName = $"{GetBrowser(Request.Headers["User-Agent"].ToString())} - {employee.City}/{employee.State}"
        };

        _db.EmployeeSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Funcionário {FullName} (ID: {Id}) fez login com sucesso",
            employee.FullName, employee.Id);

        return Ok(new
        {
            token = session.Token,
            expiresAt = session.ExpiresAt,
            requiresPasswordChange = employee.RequirePasswordChange,
            requiresTwoFactor = employee.TwoFactorEnabled && !session.TwoFactorVerified,
            employee = new
            {
                id = employee.Id,
                fullName = employee.FullName,
                socialName = employee.SocialName,
                email = employee.Email,
                jobPosition = employee.JobPosition?.Name,
                jobPositionCode = employee.JobPosition?.Code,
                hierarchyLevel = employee.JobPosition?.HierarchyLevel,
                establishment = new
                {
                    id = employee.Establishment.Id,
                    name = employee.Establishment.NomeFantasia,
                    cnpj = employee.Establishment.Cnpj
                }
            }
        });

        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Erro ao atualizar banco de dados durante login");
            return StatusCode(500, new
            {
                error = "Erro ao processar login. Tente novamente.",
                details = "Problema ao salvar dados no banco"
            });
        }
    }

    // ==================== CRIAR FUNCIONÁRIO ====================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        // TODO: Verificar permissões do token (implementar na Fase 2)
        // if (!await HasPermission("employees.create")) return Forbid();

        // Validar e limpar CPF
        var cpf = DocumentValidator.RemoveFormatting(dto.Cpf);
        if (!DocumentValidator.IsValidCpf(cpf))
            return BadRequest(new { error = "CPF inválido" });

        // Verificar CPF duplicado
        if (await _db.Employees.AnyAsync(e => e.Cpf == cpf))
            return Conflict(new { error = "CPF já cadastrado" });

        // Validar PIS/PASEP se fornecido
        if (!string.IsNullOrEmpty(dto.PisPasep))
        {
            var pis = DocumentValidator.RemoveFormatting(dto.PisPasep);
            if (!DocumentValidator.IsValidPis(pis))
                return BadRequest(new { error = "PIS/PASEP inválido" });
            dto.PisPasep = pis;
        }

        // Validar senha
        var (isPasswordValid, passwordErrors) = PasswordValidator.ValidatePassword(dto.Password);
        if (!isPasswordValid)
            return BadRequest(new { error = "Senha inválida", details = passwordErrors });

        // Verificar se cargo existe
        var jobPosition = await _db.JobPositions
            .FirstOrDefaultAsync(jp => jp.Id == dto.JobPositionId
                && jp.EstablishmentId == dto.EstablishmentId);

        if (jobPosition == null)
            return BadRequest(new { error = "Cargo não encontrado" });

        // Criar funcionário
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            EstablishmentId = dto.EstablishmentId,
            JobPositionId = dto.JobPositionId,

            // Dados Pessoais
            FullName = dto.FullName,
            SocialName = dto.SocialName,
            Cpf = cpf,
            Rg = dto.Rg,
            RgIssuer = dto.RgIssuer,
            RgIssueDate = dto.RgIssueDate,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Nationality = dto.Nationality ?? "Brasileira",
            PlaceOfBirth = dto.PlaceOfBirth,
            MaritalStatus = dto.MaritalStatus,

            // Documentos Trabalhistas
            Ctps = dto.Ctps,
            CtpsSeries = dto.CtpsSeries,
            CtpsUf = dto.CtpsUf,
            CtpsIssueDate = dto.CtpsIssueDate,
            PisPasep = dto.PisPasep,
            VoterRegistration = dto.VoterRegistration,
            MilitaryService = dto.MilitaryService,
            DriverLicense = dto.DriverLicense,
            DriverLicenseCategory = dto.DriverLicenseCategory,
            DriverLicenseExpiry = dto.DriverLicenseExpiry,

            // Contatos
            Phone = dto.Phone,
            WhatsApp = dto.WhatsApp,
            Email = dto.Email,

            // Endereço
            Street = dto.Street,
            Number = dto.Number,
            Complement = dto.Complement,
            Neighborhood = dto.Neighborhood,
            City = dto.City,
            State = dto.State,
            PostalCode = DocumentValidator.RemoveFormatting(dto.PostalCode),

            // Dados Trabalhistas
            HireDate = dto.HireDate,
            ContractType = dto.ContractType ?? "CLT",
            WorkShift = dto.WorkShift,
            Salary = dto.Salary,
            Department = dto.Department,

            // Dados Bancários
            BankCode = dto.BankCode,
            BankName = dto.BankName,
            BankBranch = dto.BankBranch,
            BankAccount = dto.BankAccount,
            BankAccountType = dto.BankAccountType,
            BankAccountDigit = dto.BankAccountDigit,

            // Contato de Emergência
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactRelationship = dto.EmergencyContactRelationship,
            EmergencyContactPhone = dto.EmergencyContactPhone,

            // Dependentes
            DependentsCount = dto.DependentsCount,

            // Status
            Status = "Ativo",
            ProbationEndDate = dto.HireDate.AddDays(dto.ProbationDays ?? 90),

            // Segurança
            PasswordHash = Argon2.Hash(dto.Password),
            PasswordAlgorithm = "argon2id-v1",
            PasswordCreatedAt = DateTime.UtcNow,
            RequirePasswordChange = true, // Primeira senha é temporária
            TwoFactorEnabled = false,
            FailedLoginAttempts = 0,

            // Auditoria
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000000") // TODO: Pegar do token na Fase 2
        };

        _db.Employees.Add(employee);

        // Criar histórico inicial de cargo
        var jobHistory = new EmployeeJobHistory
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            JobPositionId = employee.JobPositionId,
            StartDate = employee.HireDate,
            IsCurrent = true,
            SalaryAtTime = employee.Salary,
            ChangeReason = "Contratação",
            Notes = "Admissão inicial",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = employee.CreatedBy
        };

        _db.EmployeeJobHistories.Add(jobHistory);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Funcionário {FullName} (CPF: {Cpf}) cadastrado com sucesso",
            employee.FullName, DocumentValidator.FormatCpf(employee.Cpf));

        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, new
        {
            id = employee.Id,
            fullName = employee.FullName,
            cpf = DocumentValidator.FormatCpf(employee.Cpf),
            email = employee.Email,
            jobPosition = jobPosition.Name,
            status = employee.Status,
            hireDate = employee.HireDate
        });
    }

    // ==================== BUSCAR FUNCIONÁRIO POR ID ====================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = await _db.Employees
            .Include(e => e.Establishment)
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound();

        // TODO: Verificar permissões (se pode ver dados de outros funcionários)

        return Ok(new
        {
            id = employee.Id,
            establishmentId = employee.EstablishmentId,
            establishmentName = employee.Establishment?.NomeFantasia,
            jobPositionId = employee.JobPositionId,
            jobPositionName = employee.JobPosition?.Name,

            // Dados Pessoais
            fullName = employee.FullName,
            socialName = employee.SocialName,
            cpf = DocumentValidator.FormatCpf(employee.Cpf),
            rg = employee.Rg,
            rgIssuer = employee.RgIssuer,
            rgIssueDate = employee.RgIssueDate,
            dateOfBirth = employee.DateOfBirth,
            gender = employee.Gender,
            nationality = employee.Nationality,
            placeOfBirth = employee.PlaceOfBirth,
            maritalStatus = employee.MaritalStatus,

            // Contatos
            phone = employee.Phone,
            whatsApp = employee.WhatsApp,
            email = employee.Email,

            // Endereço
            address = new
            {
                street = employee.Street,
                number = employee.Number,
                complement = employee.Complement,
                neighborhood = employee.Neighborhood,
                city = employee.City,
                state = employee.State,
                postalCode = employee.PostalCode
            },

            // Dados Trabalhistas
            hireDate = employee.HireDate,
            terminationDate = employee.TerminationDate,
            contractType = employee.ContractType,
            workShift = employee.WorkShift,
            salary = employee.Salary,
            department = employee.Department,

            // Status
            status = employee.Status,
            statusNotes = employee.StatusNotes,
            probationEndDate = employee.ProbationEndDate,

            // Auditoria
            createdAt = employee.CreatedAt,
            updatedAt = employee.UpdatedAt
        });
    }

    // ==================== LISTAR FUNCIONÁRIOS ====================
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? establishmentId,
        [FromQuery] string? status,
        [FromQuery] Guid? jobPositionId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _db.Employees
            .Include(e => e.Establishment)
            .Include(e => e.JobPosition)
            .AsQueryable();

        // Filtros
        if (establishmentId.HasValue)
            query = query.Where(e => e.EstablishmentId == establishmentId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        if (jobPositionId.HasValue)
            query = query.Where(e => e.JobPositionId == jobPositionId.Value);

        var total = await query.CountAsync();

        var employees = await query
            .OrderBy(e => e.FullName)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))
            .Select(e => new
            {
                id = e.Id,
                fullName = e.FullName,
                cpf = DocumentValidator.FormatCpf(e.Cpf),
                email = e.Email,
                jobPosition = e.JobPosition!.Name,
                status = e.Status,
                hireDate = e.HireDate,
                establishment = e.Establishment!.NomeFantasia
            })
            .ToListAsync();

        return Ok(new
        {
            total,
            skip,
            take,
            items = employees
        });
    }

    // ==================== ATUALIZAR FUNCIONÁRIO ====================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto dto)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound();

        // Atualizar apenas campos fornecidos (não nulos)
        if (!string.IsNullOrWhiteSpace(dto.FullName))
            employee.FullName = dto.FullName;

        if (dto.SocialName != null)
            employee.SocialName = dto.SocialName;

        if (!string.IsNullOrWhiteSpace(dto.Email))
            employee.Email = dto.Email;

        if (!string.IsNullOrWhiteSpace(dto.Phone))
            employee.Phone = dto.Phone;

        if (dto.WhatsApp != null)
            employee.WhatsApp = dto.WhatsApp;

        // Endereço
        if (!string.IsNullOrWhiteSpace(dto.Street))
            employee.Street = dto.Street;

        if (!string.IsNullOrWhiteSpace(dto.Number))
            employee.Number = dto.Number;

        if (dto.Complement != null)
            employee.Complement = dto.Complement;

        if (!string.IsNullOrWhiteSpace(dto.Neighborhood))
            employee.Neighborhood = dto.Neighborhood;

        if (!string.IsNullOrWhiteSpace(dto.City))
            employee.City = dto.City;

        if (!string.IsNullOrWhiteSpace(dto.State))
            employee.State = dto.State;

        if (!string.IsNullOrWhiteSpace(dto.PostalCode))
            employee.PostalCode = DocumentValidator.RemoveFormatting(dto.PostalCode);

        // Dados Bancários
        if (dto.BankCode != null) employee.BankCode = dto.BankCode;
        if (dto.BankName != null) employee.BankName = dto.BankName;
        if (dto.BankBranch != null) employee.BankBranch = dto.BankBranch;
        if (dto.BankAccount != null) employee.BankAccount = dto.BankAccount;
        if (dto.BankAccountType != null) employee.BankAccountType = dto.BankAccountType;
        if (dto.BankAccountDigit != null) employee.BankAccountDigit = dto.BankAccountDigit;

        // Salário
        if (dto.Salary.HasValue && dto.Salary.Value > 0)
            employee.Salary = dto.Salary.Value;

        if (dto.Department != null)
            employee.Department = dto.Department;

        if (dto.Status != null)
            employee.Status = dto.Status;

        // Contato de Emergência
        if (dto.EmergencyContactName != null)
            employee.EmergencyContactName = dto.EmergencyContactName;

        if (dto.EmergencyContactRelationship != null)
            employee.EmergencyContactRelationship = dto.EmergencyContactRelationship;

        if (dto.EmergencyContactPhone != null)
            employee.EmergencyContactPhone = dto.EmergencyContactPhone;

        if (dto.DependentsCount.HasValue)
            employee.DependentsCount = dto.DependentsCount.Value;

        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = Guid.Parse("00000000-0000-0000-0000-000000000000"); // TODO: Token

        await _db.SaveChangesAsync();

        _logger.LogInformation("Funcionário {FullName} (ID: {Id}) atualizado",
            employee.FullName, employee.Id);

        return Ok(new { message = "Funcionário atualizado com sucesso" });
    }

    // ==================== GERAR HASH DE SENHA ====================
    [HttpPost("generate-hash")]
    [AllowAnonymous]
    public IActionResult GenerateHash([FromBody] GenerateHashDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.Password))
                return BadRequest(new { error = "Senha não pode ser vazia" });

            var hash = Argon2.Hash(dto.Password);

            return Ok(new
            {
                success = true,
                password = dto.Password,
                hash = hash,
                algorithm = "argon2id-v1",
                sqlUpdate = $@"UPDATE employees 
            SET ""PasswordHash"" = '{hash}',
                ""PasswordCreatedAt"" = CURRENT_TIMESTAMP,
                ""PasswordAlgorithm"" = 'argon2id-v1',
                ""RequirePasswordChange"" = false,
                ""FailedLoginAttempts"" = 0,
                ""LockedUntil"" = NULL
            WHERE ""Cpf"" = 'CPF_AQUI';"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao gerar hash de senha");
                        return StatusCode(500, new { error = "Erro ao gerar hash", details = ex.Message });
                    }
    }

    // ==================== HELPERS ====================
    private string GetDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)) return "Mobile";
        if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase)) return "Tablet";
        return "Desktop";
    }

    private string GetBrowser(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";
        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        return "Unknown";
    }

    // ==================== DTOs ====================
    public class GenerateHashDto
    {
        public string Password { get; set; } = "";
    }

    // ==================== DTOs ====================
    public class LoginDto
    {
        public string Cpf { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class CreateEmployeeDto
    {
        public Guid EstablishmentId { get; set; }
        public Guid JobPositionId { get; set; }

        // Dados Pessoais
        public string FullName { get; set; } = "";
        public string? SocialName { get; set; }
        public string Cpf { get; set; } = "";
        public string? Rg { get; set; }
        public string? RgIssuer { get; set; }
        public DateOnly? RgIssueDate { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? MaritalStatus { get; set; }

        // Documentos Trabalhistas
        public string? Ctps { get; set; }
        public string? CtpsSeries { get; set; }
        public string? CtpsUf { get; set; }
        public DateOnly? CtpsIssueDate { get; set; }
        public string? PisPasep { get; set; }
        public string? VoterRegistration { get; set; }
        public string? MilitaryService { get; set; }
        public string? DriverLicense { get; set; }
        public string? DriverLicenseCategory { get; set; }
        public DateOnly? DriverLicenseExpiry { get; set; }

        // Contatos
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }
        public string Email { get; set; } = "";

        // Endereço
        public string Street { get; set; } = "";
        public string Number { get; set; } = "";
        public string? Complement { get; set; }
        public string Neighborhood { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string PostalCode { get; set; } = "";

        // Dados Trabalhistas
        public DateOnly HireDate { get; set; }
        public string? ContractType { get; set; }
        public string? WorkShift { get; set; }
        public decimal Salary { get; set; }
        public string? Department { get; set; }

        // Dados Bancários
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? BankAccount { get; set; }
        public string? BankAccountType { get; set; }
        public string? BankAccountDigit { get; set; }

        // Contato de Emergência
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactRelationship { get; set; }
        public string? EmergencyContactPhone { get; set; }

        // Dependentes
        public int DependentsCount { get; set; }

        // Senha
        public string Password { get; set; } = "";

        // Período de Experiência (padrão 90 dias)
        public int? ProbationDays { get; set; }
    }

    public class UpdateEmployeeDto
    {
        public string? FullName { get; set; }
        public string? SocialName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }

        // Endereço
        public string? Street { get; set; }
        public string? Number { get; set; }
        public string? Complement { get; set; }
        public string? Neighborhood { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }

        // Dados Bancários
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? BankBranch { get; set; }
        public string? BankAccount { get; set; }
        public string? BankAccountType { get; set; }
        public string? BankAccountDigit { get; set; }

        public decimal? Salary { get; set; }
        public string? Department { get; set; }
        public string? Status { get; set; }

        // Contato de Emergência
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactRelationship { get; set; }
        public string? EmergencyContactPhone { get; set; }

        public int? DependentsCount { get; set; }
    }
}