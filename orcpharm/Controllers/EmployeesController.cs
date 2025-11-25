using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Isopoh.Cryptography.Argon2;
using Helpers;
using Microsoft.AspNetCore.Authorization;
using DTOs.Auth;
using DTOs.Employees;

namespace Controllers.Api;

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
                Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('='),
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

            Response.Cookies.Append("SessionId", session.Token, new CookieOptions
            {
                HttpOnly = true,              // Proteção contra XSS
                Secure = Request.IsHttps,     // true apenas em HTTPS
                SameSite = SameSiteMode.Lax,  // Permite navegação normal
                Expires = session.ExpiresAt,  // Mesmo tempo da sessão (8h)
                Path = "/",                   // Válido para todo o site
                IsEssential = true            // Cookie essencial para funcionamento
            });

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
        // Verificar autenticação via middleware
        var authenticatedEmployee = HttpContext.Items["Employee"] as Employee;
        if (authenticatedEmployee == null)
        {
            return Unauthorized(new { error = "Não autenticado" });
        }

        // Verificar permissões (apenas OWNER e GENERAL_MANAGER podem listar todos)
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER" };
        var jobPositionCode = authenticatedEmployee.JobPosition?.Code ?? "";

        if (!allowedCodes.Contains(jobPositionCode))
        {
            return Forbid(); // 403 Forbidden
        }

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

    [HttpPost("ChangePassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest(new { message = "Nova senha é obrigatória" });

            if (dto.NewPassword.Length < 6)
                return BadRequest(new { message = "A senha deve ter no mínimo 6 caracteres" });

            // Buscar funcionário que terá a senha alterada
            var targetEmployee = await _db.Employees
                .Include(e => e.Establishment)
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);

            if (targetEmployee == null)
                return NotFound(new { message = "Funcionário não encontrado" });

            // Obter funcionário logado do contexto (do middleware)
            var currentEmployee = HttpContext.Items["Employee"] as Employee;
            if (currentEmployee == null)
                return Unauthorized(new { message = "Usuário não autenticado" });

            // Carregar JobPosition do funcionário logado
            await _db.Entry(currentEmployee)
                .Reference(e => e.JobPosition)
                .LoadAsync();

            // Verificar permissões
            bool isOwnProfile = currentEmployee.Id == dto.EmployeeId;
            bool isManager = new[] { "OWNER", "MANAGER" }.Contains(currentEmployee.JobPosition?.Code ?? "");

            // Se não for próprio perfil e não for manager, negar
            if (!isOwnProfile && !isManager)
                return Forbid();

            // Se for próprio perfil, validar senha atual
            if (isOwnProfile)
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                    return BadRequest(new { message = "Senha atual é obrigatória" });

                if (!Argon2.Verify(targetEmployee.PasswordHash, dto.CurrentPassword))
                    return BadRequest(new { message = "Senha atual incorreta" });
            }

            // Verificar se é do mesmo estabelecimento
            if (targetEmployee.EstablishmentId != currentEmployee.EstablishmentId)
                return Forbid();

            // Gerar novo hash
            var newHash = Argon2.Hash(dto.NewPassword);

            // Atualizar senha
            targetEmployee.PasswordHash = newHash;
            targetEmployee.PasswordCreatedAt = DateTime.UtcNow;
            targetEmployee.PasswordAlgorithm = "argon2id-v1";
            targetEmployee.RequirePasswordChange = false;
            targetEmployee.FailedLoginAttempts = 0;
            targetEmployee.LockedUntil = null;
            targetEmployee.UpdatedAt = DateTime.UtcNow;
            targetEmployee.UpdatedBy = currentEmployee.Id;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Senha do funcionário {TargetId} alterada por {CurrentId}",
                targetEmployee.Id,
                currentEmployee.Id
            );

            return Ok(new { message = "Senha alterada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar senha do funcionário {EmployeeId}", dto.EmployeeId);
            return StatusCode(500, new { message = "Erro ao alterar senha", details = ex.Message });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class JobPositionsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<JobPositionsController> _logger;

        public JobPositionsController(AppDbContext db, ILogger<JobPositionsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ==================== GET: CARGOS POR ESTABELECIMENTO ====================
        /// <summary>
        /// GET: /api/JobPositions/establishment/{establishmentId}
        /// Retorna todos os cargos ativos de um estabelecimento específico
        /// </summary>
        [HttpGet("establishment/{establishmentId}")]
        public async Task<IActionResult> GetByEstablishment(Guid establishmentId)
        {
            try
            {
                _logger.LogInformation("Buscando cargos para estabelecimento {EstablishmentId}", establishmentId);

                var jobPositions = await _db.JobPositions
                    .Where(jp => jp.EstablishmentId == establishmentId && jp.IsActive)
                    .OrderBy(jp => jp.HierarchyLevel)
                    .ThenBy(jp => jp.Name)
                    .Select(jp => new
                    {
                        id = jp.Id,
                        code = jp.Code,
                        name = jp.Name,
                        description = jp.Description,
                        hierarchyLevel = jp.HierarchyLevel,
                        requiresCertification = jp.RequiresCertification,
                        requiredCertification = jp.RequiredCertification,
                        requiredEducation = jp.RequiredEducation,
                        suggestedSalaryMin = jp.SuggestedSalaryMin,
                        suggestedSalaryMax = jp.SuggestedSalaryMax,
                        responsibilities = jp.Responsibilities,
                        isSystemDefault = jp.IsSystemDefault
                    })
                    .ToListAsync();

                if (!jobPositions.Any())
                {
                    _logger.LogWarning("Nenhum cargo encontrado para estabelecimento {EstablishmentId}", establishmentId);
                    return Ok(new { data = new List<object>(), message = "Nenhum cargo cadastrado. Por favor, cadastre os cargos primeiro." });
                }

                _logger.LogInformation("Retornando {Count} cargos para estabelecimento {EstablishmentId}",
                    jobPositions.Count, establishmentId);

                return Ok(new { data = jobPositions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cargos do estabelecimento {EstablishmentId}", establishmentId);
                return StatusCode(500, new { error = "Erro ao buscar cargos", details = ex.Message });
            }
        }

        // ==================== GET: TODOS OS CARGOS ====================
        /// <summary>
        /// GET: /api/JobPositions
        /// Retorna todos os cargos ativos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var jobPositions = await _db.JobPositions
                    .Where(jp => jp.IsActive)
                    .OrderBy(jp => jp.HierarchyLevel)
                    .ThenBy(jp => jp.Name)
                    .Select(jp => new
                    {
                        id = jp.Id,
                        code = jp.Code,
                        name = jp.Name,
                        description = jp.Description,
                        hierarchyLevel = jp.HierarchyLevel,
                        establishmentId = jp.EstablishmentId,
                        requiresCertification = jp.RequiresCertification,
                        requiredCertification = jp.RequiredCertification,
                        requiredEducation = jp.RequiredEducation,
                        suggestedSalaryMin = jp.SuggestedSalaryMin,
                        suggestedSalaryMax = jp.SuggestedSalaryMax
                    })
                    .ToListAsync();

                return Ok(new { data = jobPositions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar todos os cargos");
                return StatusCode(500, new { error = "Erro ao buscar cargos", details = ex.Message });
            }
        }

        // ==================== GET: CARGO POR ID ====================
        /// <summary>
        /// GET: /api/JobPositions/{id}
        /// Retorna um cargo específico por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var jobPosition = await _db.JobPositions
                    .Where(jp => jp.Id == id)
                    .Select(jp => new
                    {
                        id = jp.Id,
                        code = jp.Code,
                        name = jp.Name,
                        description = jp.Description,
                        hierarchyLevel = jp.HierarchyLevel,
                        establishmentId = jp.EstablishmentId,
                        requiresCertification = jp.RequiresCertification,
                        requiredCertification = jp.RequiredCertification,
                        requiredEducation = jp.RequiredEducation,
                        responsibilities = jp.Responsibilities,
                        suggestedSalaryMin = jp.SuggestedSalaryMin,
                        suggestedSalaryMax = jp.SuggestedSalaryMax,
                        salaryType = jp.SalaryType,
                        isActive = jp.IsActive,
                        isSystemDefault = jp.IsSystemDefault
                    })
                    .FirstOrDefaultAsync();

                if (jobPosition == null)
                {
                    return NotFound(new { error = "Cargo não encontrado" });
                }

                return Ok(jobPosition);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar cargo {JobPositionId}", id);
                return StatusCode(500, new { error = "Erro ao buscar cargo", details = ex.Message });
            }
        }

        // ==================== POST: CRIAR CARGO ====================
        /// <summary>
        /// POST: /api/JobPositions
        /// Cria um novo cargo
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateJobPositionDto dto)
        {
            try
            {
                // Validar se o estabelecimento existe
                var establishmentExists = await _db.Establishments
                    .AnyAsync(e => e.Id == dto.EstablishmentId);

                if (!establishmentExists)
                {
                    return BadRequest(new { error = "Estabelecimento não encontrado" });
                }

                // Verificar se já existe um cargo com o mesmo Code no estabelecimento
                var codeExists = await _db.JobPositions
                    .AnyAsync(jp => jp.EstablishmentId == dto.EstablishmentId &&
                                   jp.Code == dto.Code);

                if (codeExists)
                {
                    return BadRequest(new { error = "Já existe um cargo com este código neste estabelecimento" });
                }

                var jobPosition = new JobPosition
                {
                    Id = Guid.NewGuid(),
                    EstablishmentId = dto.EstablishmentId,
                    Code = dto.Code,
                    Name = dto.Name,
                    Description = dto.Description,
                    HierarchyLevel = dto.HierarchyLevel,
                    RequiresCertification = dto.RequiresCertification,
                    RequiredCertification = dto.RequiredCertification,
                    RequiredEducation = dto.RequiredEducation,
                    Responsibilities = dto.Responsibilities,
                    SuggestedSalaryMin = dto.SuggestedSalaryMin,
                    SuggestedSalaryMax = dto.SuggestedSalaryMax,
                    SalaryType = dto.SalaryType ?? "MENSAL",
                    IsActive = true,
                    IsSystemDefault = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.JobPositions.Add(jobPosition);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Cargo {Code} criado com sucesso (ID: {Id})", jobPosition.Code, jobPosition.Id);

                return CreatedAtAction(nameof(GetById), new { id = jobPosition.Id }, new
                {
                    id = jobPosition.Id,
                    code = jobPosition.Code,
                    name = jobPosition.Name,
                    message = "Cargo criado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar cargo");
                return StatusCode(500, new { error = "Erro ao criar cargo", details = ex.Message });
            }
        }

        // ==================== PUT: ATUALIZAR CARGO ====================
        /// <summary>
        /// PUT: /api/JobPositions/{id}
        /// Atualiza um cargo existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobPositionDto dto)
        {
            try
            {
                var jobPosition = await _db.JobPositions.FindAsync(id);

                if (jobPosition == null)
                {
                    return NotFound(new { error = "Cargo não encontrado" });
                }

                // Atualizar campos
                jobPosition.Name = dto.Name ?? jobPosition.Name;
                jobPosition.Description = dto.Description ?? jobPosition.Description;
                jobPosition.RequiredEducation = dto.RequiredEducation ?? jobPosition.RequiredEducation;
                jobPosition.Responsibilities = dto.Responsibilities ?? jobPosition.Responsibilities;
                jobPosition.SuggestedSalaryMin = dto.SuggestedSalaryMin ?? jobPosition.SuggestedSalaryMin;
                jobPosition.SuggestedSalaryMax = dto.SuggestedSalaryMax ?? jobPosition.SuggestedSalaryMax;

                if (dto.RequiresCertification.HasValue)
                    jobPosition.RequiresCertification = dto.RequiresCertification.Value;

                if (dto.RequiredCertification != null)
                    jobPosition.RequiredCertification = dto.RequiredCertification;

                if (dto.IsActive.HasValue)
                    jobPosition.IsActive = dto.IsActive.Value;

                jobPosition.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Cargo {Code} atualizado com sucesso (ID: {Id})", jobPosition.Code, jobPosition.Id);

                return Ok(new { message = "Cargo atualizado com sucesso", id = jobPosition.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar cargo {JobPositionId}", id);
                return StatusCode(500, new { error = "Erro ao atualizar cargo", details = ex.Message });
            }
        }

        // ==================== DELETE: DESATIVAR CARGO ====================
        /// <summary>
        /// DELETE: /api/JobPositions/{id}
        /// Desativa um cargo (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var jobPosition = await _db.JobPositions.FindAsync(id);

                if (jobPosition == null)
                {
                    return NotFound(new { error = "Cargo não encontrado" });
                }

                // Verificar se há funcionários com este cargo
                var hasEmployees = await _db.Employees
                    .AnyAsync(e => e.JobPositionId == id);

                if (hasEmployees)
                {
                    return BadRequest(new { error = "Não é possível desativar este cargo pois há funcionários ativos vinculados a ele" });
                }

                jobPosition.IsActive = false;
                jobPosition.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Cargo {Code} desativado com sucesso (ID: {Id})", jobPosition.Code, jobPosition.Id);

                return Ok(new { message = "Cargo desativado com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desativar cargo {JobPositionId}", id);
                return StatusCode(500, new { error = "Erro ao desativar cargo", details = ex.Message });
            }
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

}