using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Pharmacy;
using System.ComponentModel.DataAnnotations;

namespace orcpharm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(AppDbContext db, ILogger<SuppliersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== CRUD SUPPLIERS ====================

    /// <summary>
    /// Lista todos os fornecedores do estabelecimento
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? classification,
        [FromQuery] bool? isQualified,
        [FromQuery] bool? afeExpiring,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        try
        {
            var query = _db.Suppliers
                .Where(s => s.EstablishmentId == employee.EstablishmentId && s.IsActive);

            // Filtros
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s =>
                    s.CompanyName.Contains(search) ||
                    (s.TradeName != null && s.TradeName.Contains(search)) ||
                    s.Cnpj.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            if (!string.IsNullOrEmpty(classification))
                query = query.Where(s => s.Classification == classification);

            if (isQualified.HasValue)
                query = query.Where(s => s.IsQualified == isQualified.Value);

            // Filtro de AFE expirando (próximos 30 dias)
            if (afeExpiring.HasValue && afeExpiring.Value)
            {
                var expiryDate = DateTime.UtcNow.AddDays(30);
                query = query.Where(s =>
                    s.AfeExpiryDate.HasValue &&
                    s.AfeExpiryDate.Value <= expiryDate &&
                    s.AfeExpiryDate.Value > DateTime.UtcNow);
            }

            var totalRecords = await query.CountAsync();

            var suppliers = await query
                .OrderBy(s => s.CompanyName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.CompanyName,
                    s.TradeName,
                    s.Cnpj,
                    s.Status,
                    s.Classification,
                    s.Rating,
                    s.IsQualified,
                    s.IsPreferred,
                    s.City,
                    s.State,
                    s.Phone,
                    s.Email,
                    s.TotalOrders,
                    s.NonConformitiesCount,
                    s.LastOrderDate,
                    s.LastEvaluationDate,
                    s.CreatedAt,
                    ContactsCount = s.Contacts!.Count(c => c.IsActive),
                    CertificatesCount = s.Certificates!.Count(c => c.IsActive)
                })
                .ToListAsync();

            return Ok(new
            {
                data = suppliers,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalRecords,
                    totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                },
                filters = new { search, status, classification, isQualified, afeExpiring }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar fornecedores");
            return StatusCode(500, new { error = "Erro ao listar fornecedores" });
        }
    }

    /// <summary>
    /// Busca fornecedor por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var supplier = await _db.Suppliers
                .Include(s => s.Contacts!.Where(c => c.IsActive))
                .Include(s => s.Certificates!.Where(c => c.IsActive))
                .Include(s => s.Evaluations)
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            return Ok(new
            {
                supplier.Id,
                supplier.CompanyName,
                supplier.TradeName,
                supplier.Cnpj,
                supplier.StateRegistration,
                supplier.MunicipalRegistration,
                address = new
                {
                    supplier.Street,
                    supplier.Number,
                    supplier.Complement,
                    supplier.Neighborhood,
                    supplier.City,
                    supplier.State,
                    supplier.PostalCode,
                    supplier.Country
                },
                contact = new
                {
                    supplier.Phone,
                    supplier.WhatsApp,
                    supplier.Email,
                    supplier.Website
                },
                supplier.Status,
                supplier.Classification,
                supplier.Rating,
                supplier.IsQualified,
                supplier.QualifiedAt,
                supplier.IsPreferred,
                supplier.AverageDeliveryTime,
                supplier.PaymentTermDays,
                supplier.MinimumOrderValue,
                supplier.Notes,
                supplier.ProductTypes,
                certifications = new
                {
                    supplier.HasGmpCertificate,
                    supplier.HasIsoCertificate,
                    supplier.HasAnvisaAuthorization
                },
                statistics = new
                {
                    supplier.TotalOrders,
                    supplier.NonConformitiesCount,
                    supplier.LastOrderDate,
                    supplier.LastEvaluationDate
                },
                contacts = supplier.Contacts,
                certificates = supplier.Certificates,
                evaluations = supplier.Evaluations!.OrderByDescending(e => e.EvaluationDate).Take(5),
                supplier.CreatedAt,
                supplier.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao buscar fornecedor" });
        }
    }

    /// <summary>
    /// Cria novo fornecedor
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        // Validar CNPJ
        var cnpj = RemoveFormatting(dto.Cnpj);
        if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14)
            return BadRequest(new { error = "CNPJ inválido" });

        // Verificar se já existe fornecedor com mesmo CNPJ
        var existingSupplier = await _db.Suppliers
            .Where(s => s.EstablishmentId == employee.EstablishmentId && s.Cnpj == cnpj && s.IsActive)
            .FirstOrDefaultAsync();

        if (existingSupplier != null)
            return BadRequest(new { error = "Já existe um fornecedor cadastrado com este CNPJ" });

        try
        {
            var supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                EstablishmentId = employee.EstablishmentId,
                CompanyName = dto.CompanyName.Trim(),
                TradeName = dto.TradeName?.Trim(),
                Cnpj = cnpj,
                StateRegistration = dto.StateRegistration?.Trim(),
                Street = dto.Street?.Trim(),
                Number = dto.Number?.Trim(),
                Complement = dto.Complement?.Trim(),
                Neighborhood = dto.Neighborhood?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                PostalCode = RemoveFormatting(dto.PostalCode),
                Country = dto.Country?.Trim() ?? "Brasil",
                Phone = dto.Phone?.Trim(),
                WhatsApp = dto.WhatsApp?.Trim(),
                Email = dto.Email?.Trim(),
                Website = dto.Website?.Trim(),
                AfeNumber = dto.AfeNumber?.Trim(),
                AfeExpiryDate = dto.AfeExpiryDate,
                SuppliesControlled = dto.SuppliesControlled,
                SuppliesAntibiotics = dto.SuppliesAntibiotics,
                Status = "Em Avaliação",
                Notes = dto.Notes?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employee.Id,
                UpdatedByEmployeeId = employee.Id
            };

            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Fornecedor {Name} criado por {EmployeeName}",
                supplier.CompanyName, employee.FullName);

            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, new
            {
                supplier.Id,
                supplier.CompanyName,
                supplier.TradeName,
                supplier.Cnpj,
                supplier.Status,
                supplier.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar fornecedor");
            return StatusCode(500, new { error = "Erro ao criar fornecedor" });
        }
    }

    /// <summary>
    /// Atualiza fornecedor
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            // Atualizar campos fornecidos
            if (!string.IsNullOrEmpty(dto.Name))
                supplier.CompanyName = dto.Name.Trim();

            if (dto.TradeName != null)
                supplier.TradeName = dto.TradeName.Trim();

            if (!string.IsNullOrEmpty(dto.StateRegistration))
                supplier.StateRegistration = dto.StateRegistration.Trim();

            if (!string.IsNullOrEmpty(dto.Street))
                supplier.Street = dto.Street.Trim();

            if (!string.IsNullOrEmpty(dto.Number))
                supplier.Number = dto.Number.Trim();

            if (dto.Complement != null)
                supplier.Complement = dto.Complement.Trim();

            if (!string.IsNullOrEmpty(dto.Neighborhood))
                supplier.Neighborhood = dto.Neighborhood.Trim();

            if (!string.IsNullOrEmpty(dto.City))
                supplier.City = dto.City.Trim();

            if (!string.IsNullOrEmpty(dto.State))
                supplier.State = dto.State.Trim();

            if (!string.IsNullOrEmpty(dto.PostalCode))
                supplier.PostalCode = RemoveFormatting(dto.PostalCode);

            if (!string.IsNullOrEmpty(dto.Phone))
                supplier.Phone = dto.Phone.Trim();

            if (!string.IsNullOrEmpty(dto.WhatsApp))
                supplier.WhatsApp = dto.WhatsApp.Trim();

            if (!string.IsNullOrEmpty(dto.Email))
                supplier.Email = dto.Email.Trim();

            if (!string.IsNullOrEmpty(dto.Website))
                supplier.Website = dto.Website.Trim();

            if (!string.IsNullOrEmpty(dto.AfeNumber))
                supplier.AfeNumber = dto.AfeNumber.Trim();

            if (dto.AfeExpiryDate.HasValue)
                supplier.AfeExpiryDate = dto.AfeExpiryDate;

            if (dto.SuppliesControlled.HasValue)
                supplier.SuppliesControlled = dto.SuppliesControlled.Value;

            if (dto.SuppliesAntibiotics.HasValue)
                supplier.SuppliesAntibiotics = dto.SuppliesAntibiotics.Value;

            if (dto.Notes != null)
                supplier.Notes = dto.Notes.Trim();

            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Fornecedor {Name} atualizado por {EmployeeName}",
                supplier.CompanyName, employee.FullName);

            return Ok(new
            {
                supplier.Id,
                supplier.CompanyName,
                supplier.TradeName,
                supplier.Status,
                supplier.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao atualizar fornecedor" });
        }
    }

    /// <summary>
    /// Inativa fornecedor (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            supplier.IsActive = false;
            supplier.Status = "Inativo";
            supplier.InactivatedAt = DateTime.UtcNow;
            supplier.InactivatedByEmployeeId = employee.Id;
            supplier.InactivationReason = dto.Reason?.Trim();
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "Fornecedor {Name} inativado por {EmployeeName}. Motivo: {Reason}",
                supplier.CompanyName, employee.FullName, dto.Reason);

            return Ok(new
            {
                message = "Fornecedor inativado com sucesso",
                supplier.Id,
                supplier.CompanyName,
                supplier.InactivatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inativar fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao inativar fornecedor" });
        }
    }

    /// <summary>
    /// Qualifica/Homologa fornecedor
    /// </summary>
    [HttpPut("{id}/qualify")]
    public async Task<IActionResult> Qualify(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasSupplierQualificationPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para qualificar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            supplier.IsQualified = true;
            supplier.QualifiedAt = DateTime.UtcNow;
            supplier.Status = "Ativo";
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Fornecedor {Name} qualificado por {EmployeeName}",
                supplier.CompanyName, employee.FullName);

            return Ok(new
            {
                message = "Fornecedor qualificado com sucesso",
                supplier.Id,
                supplier.CompanyName,
                supplier.IsQualified,
                supplier.QualifiedAt,
                supplier.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao qualificar fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao qualificar fornecedor" });
        }
    }

    /// <summary>
    /// Bloqueia fornecedor
    /// </summary>
    [HttpPut("{id}/block")]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasSupplierQualificationPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para bloquear fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            supplier.Status = "Bloqueado";
            supplier.IsQualified = false;
            supplier.Notes = $"BLOQUEADO em {DateTime.UtcNow:dd/MM/yyyy}: {dto.Reason}\n\n{supplier.Notes}";
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "Fornecedor {Name} bloqueado por {EmployeeName}. Motivo: {Reason}",
                supplier.CompanyName, employee.FullName, dto.Reason);

            return Ok(new
            {
                message = "Fornecedor bloqueado com sucesso",
                supplier.Id,
                supplier.CompanyName,
                supplier.Status,
                reason = dto.Reason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao bloquear fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao bloquear fornecedor" });
        }
    }

    /// <summary>
    /// Estatísticas de fornecedores
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var suppliers = await _db.Suppliers
                .Where(s => s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var stats = new
            {
                totalSuppliers = suppliers.Count,
                byStatus = suppliers.GroupBy(s => s.Status)
                    .Select(g => new { status = g.Key, count = g.Count() })
                    .ToList(),
                byClassification = suppliers.Where(s => s.Classification != null)
                    .GroupBy(s => s.Classification)
                    .Select(g => new { classification = g.Key, count = g.Count() })
                    .ToList(),
                qualified = suppliers.Count(s => s.IsQualified),
                notQualified = suppliers.Count(s => !s.IsQualified),
                preferred = suppliers.Count(s => s.IsPreferred),
                averageRating = suppliers.Any(s => s.Rating.HasValue)
                    ? suppliers.Where(s => s.Rating.HasValue).Average(s => s.Rating!.Value)
                    : (decimal?)null,
                suppliesControlled = suppliers.Count(s => s.SuppliesControlled),
                suppliesAntibiotics = suppliers.Count(s => s.SuppliesAntibiotics),
                afeExpired = suppliers.Count(s => s.AfeExpiryDate.HasValue && s.AfeExpiryDate.Value < now),
                afeExpiringSoon = suppliers.Count(s => s.AfeExpiryDate.HasValue &&
                    s.AfeExpiryDate.Value > now &&
                    s.AfeExpiryDate.Value <= now.AddDays(30)),
                totalOrders = suppliers.Sum(s => s.TotalOrders),
                totalNonConformities = suppliers.Sum(s => s.NonConformitiesCount)
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas de fornecedores");
            return StatusCode(500, new { error = "Erro ao obter estatísticas" });
        }
    }

    // ==================== CONTACTS ====================

    /// <summary>
    /// Lista contatos de um fornecedor
    /// </summary>
    [HttpGet("{id}/contacts")]
    public async Task<IActionResult> GetContacts(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var contacts = await _db.SupplierContacts
                .Where(c => c.SupplierId == id && c.IsActive)
                .OrderByDescending(c => c.IsPrimary)
                .ThenBy(c => c.FullName)
                .ToListAsync();

            return Ok(new { data = contacts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar contatos do fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao listar contatos" });
        }
    }

    /// <summary>
    /// Adiciona contato ao fornecedor
    /// </summary>
    [HttpPost("{id}/contacts")]
    public async Task<IActionResult> AddContact(Guid id, [FromBody] CreateContactDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            // Se este será o contato primário, desmarcar os outros
            if (dto.IsPrimary)
            {
                var existingContacts = await _db.SupplierContacts
                    .Where(c => c.SupplierId == id && c.IsPrimary && c.IsActive)
                    .ToListAsync();

                foreach (var contact in existingContacts)
                    contact.IsPrimary = false;
            }

            var newContact = new SupplierContact
            {
                Id = Guid.NewGuid(),
                SupplierId = id,
                FullName = dto.FullName.Trim(),
                JobTitle = dto.JobTitle?.Trim(),
                Department = dto.Department?.Trim(),
                Email = dto.Email?.Trim(),
                Phone = dto.Phone?.Trim(),
                Mobile = dto.Mobile?.Trim(),
                Extension = dto.Extension?.Trim(),
                IsPrimary = dto.IsPrimary,
                IsEmergencyContact = dto.IsEmergencyContact,
                Notes = dto.Notes?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employee.Id,
                UpdatedByEmployeeId = employee.Id
            };

            _db.SupplierContacts.Add(newContact);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Contato {ContactName} adicionado ao fornecedor {SupplierName} por {EmployeeName}",
                newContact.FullName, supplier.CompanyName, employee.FullName);

            return CreatedAtAction(nameof(GetContacts), new { id }, newContact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar contato ao fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao adicionar contato" });
        }
    }

    /// <summary>
    /// Remove contato do fornecedor
    /// </summary>
    [HttpDelete("{supplierId}/contacts/{contactId}")]
    public async Task<IActionResult> DeleteContact(Guid supplierId, Guid contactId)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var contact = await _db.SupplierContacts
                .Include(c => c.Supplier)
                .Where(c => c.Id == contactId && c.SupplierId == supplierId && c.IsActive)
                .FirstOrDefaultAsync();

            if (contact == null)
                return NotFound(new { error = "Contato não encontrado" });

            if (contact.Supplier?.EstablishmentId != employee.EstablishmentId)
                return StatusCode(403, new { error = "Sem permissão para acessar este contato" });

            contact.IsActive = false;
            contact.UpdatedAt = DateTime.UtcNow;
            contact.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Contato removido com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover contato {ContactId}", contactId);
            return StatusCode(500, new { error = "Erro ao remover contato" });
        }
    }

    // ==================== CERTIFICATES ====================

    /// <summary>
    /// Lista certificados de um fornecedor
    /// </summary>
    [HttpGet("{id}/certificates")]
    public async Task<IActionResult> GetCertificates(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var certificates = await _db.SupplierCertificates
                .Where(c => c.SupplierId == id && c.IsActive)
                .OrderBy(c => c.ExpiryDate)
                .Select(c => new
                {
                    c.Id,
                    c.CertificateType,
                    c.Name,
                    c.Number,
                    c.IssuingAuthority,
                    c.IssueDate,
                    c.ExpiryDate,
                    c.Status,
                    c.IsValid,
                    c.DaysUntilExpiry,
                    c.IsExpiringSoon,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(new { data = certificates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar certificados do fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao listar certificados" });
        }
    }

    /// <summary>
    /// Adiciona certificado ao fornecedor
    /// </summary>
    [HttpPost("{id}/certificates")]
    public async Task<IActionResult> AddCertificate(Guid id, [FromBody] CreateCertificateDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var certificate = new SupplierCertificate
            {
                Id = Guid.NewGuid(),
                SupplierId = id,
                CertificateType = dto.CertificateType.ToUpper(),
                Name = dto.Name.Trim(),
                Number = dto.Number?.Trim(),
                IssuingAuthority = dto.IssuingAuthority?.Trim(),
                IssueDate = dto.IssueDate,
                ExpiryDate = dto.ExpiryDate,
                Status = dto.ExpiryDate.HasValue && dto.ExpiryDate.Value > DateTime.UtcNow ? "Válido" : "Expirado",
                Notes = dto.Notes?.Trim(),
                AlertBeforeExpiry = dto.AlertBeforeExpiry ?? true,
                AlertDaysBefore = dto.AlertDaysBefore ?? 30,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employee.Id,
                UpdatedByEmployeeId = employee.Id
            };

            _db.SupplierCertificates.Add(certificate);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Certificado {CertificateName} adicionado ao fornecedor {SupplierName}",
                certificate.Name, supplier.CompanyName);

            return CreatedAtAction(nameof(GetCertificates), new { id }, certificate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar certificado ao fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao adicionar certificado" });
        }
    }

    /// <summary>
    /// Lista certificados expirando (próximos 30 dias)
    /// </summary>
    [HttpGet("certificates/expiring")]
    public async Task<IActionResult> GetExpiringCertificates([FromQuery] int days = 30)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var expiryDate = DateTime.UtcNow.AddDays(days);

            var certificates = await _db.SupplierCertificates
                .Include(c => c.Supplier)
                .Where(c =>
                    c.Supplier!.EstablishmentId == employee.EstablishmentId &&
                    c.IsActive &&
                    c.ExpiryDate.HasValue &&
                    c.ExpiryDate.Value <= expiryDate &&
                    c.ExpiryDate.Value > DateTime.UtcNow)
                .OrderBy(c => c.ExpiryDate)
                .Select(c => new
                {
                    c.Id,
                    c.SupplierId,
                    supplierName = c.Supplier!.CompanyName,
                    c.CertificateType,
                    c.Name,
                    c.Number,
                    c.ExpiryDate,
                    c.DaysUntilExpiry,
                    c.AlertDaysBefore
                })
                .ToListAsync();

            return Ok(new
            {
                data = certificates,
                count = certificates.Count,
                daysFilter = days
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar certificados expirando");
            return StatusCode(500, new { error = "Erro ao listar certificados" });
        }
    }

    // ==================== EVALUATIONS ====================

    /// <summary>
    /// Lista avaliações de um fornecedor
    /// </summary>
    [HttpGet("{id}/evaluations")]
    public async Task<IActionResult> GetEvaluations(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var evaluations = await _db.SupplierEvaluations
                .Where(e => e.SupplierId == id)
                .OrderByDescending(e => e.EvaluationDate)
                .ToListAsync();

            return Ok(new { data = evaluations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar avaliações do fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao listar avaliações" });
        }
    }

    /// <summary>
    /// Cria avaliação de fornecedor
    /// </summary>
    [HttpPost("{id}/evaluations")]
    public async Task<IActionResult> CreateEvaluation(Guid id, [FromBody] CreateEvaluationDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!await HasSupplierQualificationPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para avaliar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            // Calcular nota geral (média das notas fornecidas)
            var scores = new List<decimal>();
            if (dto.QualityScore.HasValue) scores.Add(dto.QualityScore.Value);
            if (dto.DeliveryScore.HasValue) scores.Add(dto.DeliveryScore.Value);
            if (dto.PriceScore.HasValue) scores.Add(dto.PriceScore.Value);
            if (dto.ServiceScore.HasValue) scores.Add(dto.ServiceScore.Value);
            if (dto.DocumentationScore.HasValue) scores.Add(dto.DocumentationScore.Value);
            if (dto.ComplianceScore.HasValue) scores.Add(dto.ComplianceScore.Value);

            var overallScore = scores.Any() ? scores.Average() : 0;

            // Determinar classificação
            string? classification = overallScore >= 9.0m ? "A" :
                                    overallScore >= 7.0m ? "B" :
                                    overallScore >= 5.0m ? "C" : "D";

            var evaluation = new SupplierEvaluation
            {
                Id = Guid.NewGuid(),
                SupplierId = id,
                EvaluationDate = dto.EvaluationDate ?? DateTime.UtcNow,
                Period = dto.Period?.Trim(),
                QualityScore = dto.QualityScore,
                DeliveryScore = dto.DeliveryScore,
                PriceScore = dto.PriceScore,
                ServiceScore = dto.ServiceScore,
                DocumentationScore = dto.DocumentationScore,
                ComplianceScore = dto.ComplianceScore,
                OverallScore = overallScore,
                Classification = classification,
                TotalOrders = dto.TotalOrders ?? 0,
                OnTimeDeliveries = dto.OnTimeDeliveries ?? 0,
                LateDeliveries = dto.LateDeliveries ?? 0,
                NonConformities = dto.NonConformities ?? 0,
                Returns = dto.Returns ?? 0,
                Strengths = dto.Strengths?.Trim(),
                Weaknesses = dto.Weaknesses?.Trim(),
                CorrectiveActions = dto.CorrectiveActions?.Trim(),
                Comments = dto.Comments?.Trim(),
                Recommendation = dto.Recommendation?.Trim(),
                IsApproved = dto.IsApproved ?? true,
                EvaluatedByEmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.SupplierEvaluations.Add(evaluation);

            // Atualizar dados do fornecedor
            supplier.Rating = overallScore;
            supplier.Classification = classification;
            supplier.LastEvaluationDate = evaluation.EvaluationDate;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Avaliação criada para fornecedor {SupplierName} por {EmployeeName}. Nota: {Score}",
                supplier.CompanyName, employee.FullName, overallScore);

            return CreatedAtAction(nameof(GetEvaluations), new { id }, evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar avaliação do fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao criar avaliação" });
        }
    }

    // ==================== MÉTODOS AUXILIARES ====================

    private bool IsEmployeeActive(Employee employee)
    {
        return employee.Status == "Ativo" &&
               employee.Establishment != null &&
               employee.Establishment.IsActive;
    }

    private async Task<bool> HasSupplierManagementPermission(Employee employee)
    {
        if (employee.JobPosition == null)
        {
            await _db.Entry(employee)
                .Reference(e => e.JobPosition)
                .LoadAsync();
        }

        if (employee.JobPosition == null)
            return false;

        var allowedPositionCodes = new[]
        {
            "admin",
            "manager",
            "pharmacist_rt",
            "stock_assistant"
        };

        return allowedPositionCodes.Contains(employee.JobPosition.Code);
    }

    private async Task<bool> HasSupplierQualificationPermission(Employee employee)
    {
        if (employee.JobPosition == null)
        {
            await _db.Entry(employee)
                .Reference(e => e.JobPosition)
                .LoadAsync();
        }

        if (employee.JobPosition == null)
            return false;

        var allowedPositionCodes = new[]
        {
            "admin",
            "manager",
            "pharmacist_rt"
        };

        return allowedPositionCodes.Contains(employee.JobPosition.Code);
    }

    private static string RemoveFormatting(string? document)
    {
        if (string.IsNullOrEmpty(document))
            return string.Empty;

        return new string(document.Where(char.IsDigit).ToArray());
    }

    // ==================== DTOs ====================

    public class CreateSupplierDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TradeName { get; set; }

        [Required(ErrorMessage = "CNPJ é obrigatório")]
        [MaxLength(18)]
        public string Cnpj { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? StateRegistration { get; set; }

        [MaxLength(100)]
        public string? Street { get; set; }

        [MaxLength(10)]
        public string? Number { get; set; }

        [MaxLength(100)]
        public string? Complement { get; set; }

        [MaxLength(100)]
        public string? Neighborhood { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? WhatsApp { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(50)]
        public string? AfeNumber { get; set; }

        public DateTime? AfeExpiryDate { get; set; }

        public bool SuppliesControlled { get; set; }

        public bool SuppliesAntibiotics { get; set; }

        public string? Notes { get; set; }
    }

    public class UpdateSupplierDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(200)]
        public string? TradeName { get; set; }

        [MaxLength(20)]
        public string? StateRegistration { get; set; }

        [MaxLength(100)]
        public string? Street { get; set; }

        [MaxLength(10)]
        public string? Number { get; set; }

        [MaxLength(100)]
        public string? Complement { get; set; }

        [MaxLength(100)]
        public string? Neighborhood { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? WhatsApp { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(50)]
        public string? AfeNumber { get; set; }

        public DateTime? AfeExpiryDate { get; set; }

        public bool? SuppliesControlled { get; set; }

        public bool? SuppliesAntibiotics { get; set; }

        public string? Notes { get; set; }
    }

    public class DeleteSupplierDto
    {
        [Required(ErrorMessage = "Motivo da inativação é obrigatório")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class BlockSupplierDto
    {
        [Required(ErrorMessage = "Motivo do bloqueio é obrigatório")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class CreateContactDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        [MaxLength(10)]
        public string? Extension { get; set; }

        public bool IsPrimary { get; set; } = false;

        public bool IsEmergencyContact { get; set; } = false;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class CreateCertificateDto
    {
        [Required(ErrorMessage = "Tipo de certificado é obrigatório")]
        [MaxLength(50)]
        public string CertificateType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nome do certificado é obrigatório")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Number { get; set; }

        [MaxLength(200)]
        public string? IssuingAuthority { get; set; }

        public DateTime? IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public string? Notes { get; set; }

        public bool? AlertBeforeExpiry { get; set; } = true;

        public int? AlertDaysBefore { get; set; } = 30;
    }

    public class CreateEvaluationDto
    {
        public DateTime? EvaluationDate { get; set; }

        [MaxLength(50)]
        public string? Period { get; set; }

        [Range(0, 10)]
        public decimal? QualityScore { get; set; }

        [Range(0, 10)]
        public decimal? DeliveryScore { get; set; }

        [Range(0, 10)]
        public decimal? PriceScore { get; set; }

        [Range(0, 10)]
        public decimal? ServiceScore { get; set; }

        [Range(0, 10)]
        public decimal? DocumentationScore { get; set; }

        [Range(0, 10)]
        public decimal? ComplianceScore { get; set; }

        public int? TotalOrders { get; set; }

        public int? OnTimeDeliveries { get; set; }

        public int? LateDeliveries { get; set; }

        public int? NonConformities { get; set; }

        public int? Returns { get; set; }

        public string? Strengths { get; set; }

        public string? Weaknesses { get; set; }

        public string? CorrectiveActions { get; set; }

        public string? Comments { get; set; }

        [MaxLength(20)]
        public string? Recommendation { get; set; }

        public bool? IsApproved { get; set; } = true;
    }
}