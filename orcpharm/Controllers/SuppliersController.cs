using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Pharmacy;
using System.ComponentModel.DataAnnotations;

namespace Controllers;

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
                .OrderByDescending(s => s.IsPreferred)
                .ThenByDescending(s => s.Rating)
                .ThenBy(s => s.CompanyName)
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
                    ContactsCount = s.Contacts != null ? s.Contacts.Count(c => c.IsActive) : 0,
                    CertificatesCount = s.Certificates != null ? s.Certificates.Count(c => c.IsActive) : 0
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

            // ✅ NOVO: Buscar informações do funcionário criador
            Employee? createdByEmp = null;
            if (supplier.CreatedByEmployeeId.HasValue)
            {
                createdByEmp = await _db.Employees
                    .Include(e => e.JobPosition)
                    .FirstOrDefaultAsync(e => e.Id == supplier.CreatedByEmployeeId.Value);
            }

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
                // ✅ NOVO: Informações de AFE com status
                afe = new
                {
                    supplier.AfeNumber,
                    supplier.AfeExpiryDate,
                    Status = supplier.AfeExpiryDate.HasValue
        ? (supplier.AfeExpiryDate.Value < DateTime.UtcNow ? "Vencida" :
           supplier.AfeExpiryDate.Value < DateTime.UtcNow.AddDays(30) ? "Vencendo" : "Válida")
        : null,
                    DaysUntilExpiry = supplier.AfeExpiryDate.HasValue
        ? (supplier.AfeExpiryDate.Value - DateTime.UtcNow).Days
        : (int?)null
                },
                // ✅ NOVO: Informações de produtos
                products = new
                {
                    supplier.SuppliesControlled,
                    supplier.SuppliesAntibiotics
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
                // ✅ NOVO: Informações do funcionário criador
                createdBy = createdByEmp != null ? new
                {
                    createdByEmp.Id,
                    createdByEmp.FullName,
                    JobPosition = createdByEmp.JobPosition?.Name
                } : null,
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

        if (!HasSupplierManagementPermission(employee))
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
                MunicipalRegistration = null,
                Street = dto.Street?.Trim(),
                Number = dto.Number?.Trim(),
                Complement = dto.Complement?.Trim(),
                Neighborhood = dto.Neighborhood?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim()?.ToUpper(),
                PostalCode = RemoveFormatting(dto.PostalCode),
                Country = dto.Country?.Trim() ?? "Brasil",
                Phone = RemoveFormatting(dto.Phone),
                WhatsApp = RemoveFormatting(dto.WhatsApp),
                Email = dto.Email?.Trim()?.ToLower(),
                Website = dto.Website?.Trim(),
                Status = "ATIVO",
                // ✅ MELHORIA: Novos campos no Create
                Classification = dto.Classification?.Trim()?.ToUpper(),
                Rating = dto.Rating,
                IsQualified = dto.IsQualified,
                AverageDeliveryTime = dto.AverageDeliveryTime,
                PaymentTermDays = dto.PaymentTermDays,
                MinimumOrderValue = dto.MinimumOrderValue,
                ProductTypes = dto.ProductTypes?.Trim(),
                HasGmpCertificate = dto.HasGmpCertificate,
                HasIsoCertificate = dto.HasIsoCertificate,
                HasAnvisaAuthorization = dto.HasAnvisaAuthorization,
                AfeNumber = dto.AfeNumber?.Trim(),
                AfeExpiryDate = dto.AfeExpiryDate,
                SuppliesControlled = dto.SuppliesControlled,
                SuppliesAntibiotics = dto.SuppliesAntibiotics,
                Notes = dto.Notes?.Trim(),
                IsActive = true,
                CreatedByEmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Fornecedor {SupplierId} criado por {EmployeeId} no estabelecimento {EstablishmentId}",
                supplier.Id, employee.Id, employee.EstablishmentId);

            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, new
            {
                message = "Fornecedor criado com sucesso",
                supplierId = supplier.Id
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

        if (!HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            // Atualizar campos se fornecidos
            if (!string.IsNullOrEmpty(dto.CompanyName))
                supplier.CompanyName = dto.CompanyName.Trim();

            if (dto.TradeName != null)
                supplier.TradeName = dto.TradeName.Trim();

            if (dto.StateRegistration != null)
                supplier.StateRegistration = dto.StateRegistration.Trim();

            if (dto.Street != null)
                supplier.Street = dto.Street.Trim();

            if (dto.Number != null)
                supplier.Number = dto.Number.Trim();

            if (dto.Complement != null)
                supplier.Complement = dto.Complement.Trim();

            if (dto.Neighborhood != null)
                supplier.Neighborhood = dto.Neighborhood.Trim();

            if (dto.City != null)
                supplier.City = dto.City.Trim();

            if (dto.State != null)
                supplier.State = dto.State.Trim().ToUpper();

            if (dto.PostalCode != null)
                supplier.PostalCode = RemoveFormatting(dto.PostalCode);

            if (dto.Phone != null)
                supplier.Phone = RemoveFormatting(dto.Phone);

            if (dto.WhatsApp != null)
                supplier.WhatsApp = RemoveFormatting(dto.WhatsApp);

            if (dto.Email != null)
                supplier.Email = dto.Email.Trim().ToLower();

            if (dto.Website != null)
                supplier.Website = dto.Website.Trim();

            if (dto.AfeNumber != null)
                supplier.AfeNumber = dto.AfeNumber.Trim();

            if (dto.AfeExpiryDate.HasValue)
                supplier.AfeExpiryDate = dto.AfeExpiryDate.Value;

            if (dto.SuppliesControlled.HasValue)
                supplier.SuppliesControlled = dto.SuppliesControlled.Value;

            if (dto.SuppliesAntibiotics.HasValue)
                supplier.SuppliesAntibiotics = dto.SuppliesAntibiotics.Value;

            if (dto.Notes != null)
                supplier.Notes = dto.Notes.Trim();

            // Novos campos
            if (dto.Classification != null)
                supplier.Classification = dto.Classification.Trim().ToUpper();

            if (dto.Rating.HasValue)
                supplier.Rating = dto.Rating.Value;

            if (dto.IsQualified.HasValue)
                supplier.IsQualified = dto.IsQualified.Value;

            if (dto.IsPreferred.HasValue)
                supplier.IsPreferred = dto.IsPreferred.Value;

            if (dto.AverageDeliveryTime.HasValue)
                supplier.AverageDeliveryTime = dto.AverageDeliveryTime.Value;

            if (dto.PaymentTermDays.HasValue)
                supplier.PaymentTermDays = dto.PaymentTermDays.Value;

            if (dto.MinimumOrderValue.HasValue)
                supplier.MinimumOrderValue = dto.MinimumOrderValue.Value;

            if (dto.ProductTypes != null)
                supplier.ProductTypes = dto.ProductTypes.Trim();

            if (dto.HasGmpCertificate.HasValue)
                supplier.HasGmpCertificate = dto.HasGmpCertificate.Value;

            if (dto.HasIsoCertificate.HasValue)
                supplier.HasIsoCertificate = dto.HasIsoCertificate.Value;

            if (dto.HasAnvisaAuthorization.HasValue)
                supplier.HasAnvisaAuthorization = dto.HasAnvisaAuthorization.Value;

            supplier.UpdatedByEmployeeId = employee.Id;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Fornecedor {SupplierId} atualizado por {EmployeeId}",
                supplier.Id, employee.Id);

            return Ok(new { message = "Fornecedor atualizado com sucesso" });
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

        if (!HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            // ✅ NOVO: Validar se existem lotes ativos
            var batches = await _db.Batches
                .Where(b => b.SupplierId == id && b.Status == "APROVADO")
                .CountAsync();

            if (batches > 0)
            {
                return BadRequest(new
                {
                    error = "Não é possível desativar fornecedor com lotes ativos",
                    activeBatchesCount = batches
                });
            }

            supplier.Status = "Inativo";
            supplier.InactivatedAt = DateTime.UtcNow;
            supplier.InactivatedByEmployeeId = employee.Id;
            supplier.InactivationReason = dto.Reason?.Trim();

            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "Fornecedor {SupplierId} inativado por {EmployeeId}. Motivo: {Reason}",
                supplier.Id, employee.Id, dto.Reason);

            return Ok(new { message = "Fornecedor inativado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inativar fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao inativar fornecedor" });
        }
    }

    /// <summary>
    /// Bloqueia fornecedor temporariamente
    /// </summary>
    [HttpPost("{id}/block")]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            if (supplier.Status == "BLOQUEADO")
                return BadRequest(new { error = "Fornecedor já está bloqueado" });

            supplier.Status = "Bloqueado";
            supplier.IsQualified = false;
            supplier.BlockedReason = dto.Reason?.Trim();
            supplier.BlockedByEmployeeId = employee.Id;
            supplier.BlockedAt = DateTime.UtcNow;
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "Fornecedor {SupplierId} bloqueado por {EmployeeId}. Motivo: {Reason}",
                supplier.Id, employee.Id, dto.Reason);

            return Ok(new { message = "Fornecedor bloqueado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao bloquear fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao bloquear fornecedor" });
        }
    }

    [HttpPut("{id}/unblock")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!HasSupplierQualificationPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para desbloquear fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            if (supplier.Status != "Bloqueado")
                return BadRequest(new { error = "Fornecedor não está bloqueado" });

            supplier.Status = "Ativo";
            supplier.BlockedReason = null;
            supplier.BlockedByEmployeeId = null;
            supplier.BlockedAt = null;
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Fornecedor {Name} desbloqueado por {EmployeeName}",
                supplier.CompanyName, employee.FullName);

            return Ok(new
            {
                message = "Fornecedor desbloqueado com sucesso",
                supplier.Id,
                supplier.CompanyName,
                supplier.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desbloquear fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao desbloquear fornecedor" });
        }
    }
    

    // ==================== CONTACTS ====================

    /// <summary>
    /// Lista contatos do fornecedor
    /// </summary>
    [HttpGet("{supplierId}/contacts")]
    public async Task<IActionResult> ListContacts(Guid supplierId)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == supplierId && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var contacts = await _db.SupplierContacts
                .Where(c => c.SupplierId == supplierId && c.IsActive)
                .OrderByDescending(c => c.IsPrimary)
                .ThenBy(c => c.FullName)
                .ToListAsync();

            return Ok(new { data = contacts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar contatos do fornecedor {SupplierId}", supplierId);
            return StatusCode(500, new { error = "Erro ao listar contatos" });
        }
    }

    /// <summary>
    /// Adiciona contato ao fornecedor
    /// </summary>
    [HttpPost("{supplierId}/contacts")]
    public async Task<IActionResult> AddContact(Guid supplierId, [FromBody] CreateContactDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == supplierId && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            // Se é contato primário, remover flag dos outros
            if (dto.IsPrimary)
            {
                var existingContacts = await _db.SupplierContacts
                    .Where(c => c.SupplierId == supplierId && c.IsActive && c.IsPrimary)
                    .ToListAsync();

                foreach (var contact in existingContacts)
                    contact.IsPrimary = false;
            }

            var newContact = new SupplierContact
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                FullName = dto.FullName.Trim(),
                JobTitle = dto.JobTitle?.Trim(),
                Department = dto.Department?.Trim(),
                Email = dto.Email?.Trim()?.ToLower(),
                Phone = RemoveFormatting(dto.Phone),
                Mobile = RemoveFormatting(dto.Mobile),
                Extension = dto.Extension?.Trim(),
                IsPrimary = dto.IsPrimary,
                IsEmergencyContact = dto.IsEmergencyContact,
                Notes = dto.Notes?.Trim(),
                IsActive = true,
                CreatedByEmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupplierContacts.Add(newContact);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Contato {ContactId} adicionado ao fornecedor {SupplierId} por {EmployeeId}",
                newContact.Id, supplierId, employee.Id);

            return CreatedAtAction(nameof(ListContacts), new { supplierId }, new
            {
                message = "Contato adicionado com sucesso",
                contactId = newContact.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar contato ao fornecedor {SupplierId}", supplierId);
            return StatusCode(500, new { error = "Erro ao adicionar contato" });
        }
    }

    /// <summary>
    /// Remove contato do fornecedor
    /// </summary>
    [HttpDelete("{supplierId}/contacts/{contactId}")]
    public async Task<IActionResult> RemoveContact(Guid supplierId, Guid contactId)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var contact = await _db.SupplierContacts
                .Where(c => c.Id == contactId && c.SupplierId == supplierId && c.IsActive)
                .FirstOrDefaultAsync();

            if (contact == null)
                return NotFound(new { error = "Contato não encontrado" });

            contact.IsActive = false;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Contato {ContactId} removido do fornecedor {SupplierId} por {EmployeeId}",
                contactId, supplierId, employee.Id);

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
    /// Lista certificados do fornecedor
    /// </summary>
    [HttpGet("{supplierId}/certificates")]
    public async Task<IActionResult> ListCertificates(Guid supplierId)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == supplierId && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var certificates = await _db.SupplierCertificates
                .Where(c => c.SupplierId == supplierId && c.IsActive)
                .OrderBy(c => c.CertificateType)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.CertificateType,
                    c.Name,
                    c.Number,
                    c.IssuingAuthority,
                    c.IssueDate,
                    c.ExpiryDate,
                    Status = c.ExpiryDate.HasValue
                        ? (c.ExpiryDate.Value < DateTime.UtcNow ? "Vencido" :
                           c.ExpiryDate.Value < DateTime.UtcNow.AddDays(30) ? "Vencendo" : "Válido")
                        : "Sem Validade",
                    DaysUntilExpiry = c.ExpiryDate.HasValue
                        ? (c.ExpiryDate.Value - DateTime.UtcNow).Days
                        : (int?)null,
                    c.Notes,
                    c.AlertBeforeExpiry,
                    c.AlertDaysBefore,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(new { data = certificates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar certificados do fornecedor {SupplierId}", supplierId);
            return StatusCode(500, new { error = "Erro ao listar certificados" });
        }
    }

    /// <summary>
    /// Adiciona certificado ao fornecedor
    /// </summary>
    [HttpPost("{supplierId}/certificates")]
    public async Task<IActionResult> AddCertificate(Guid supplierId, [FromBody] CreateCertificateDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == supplierId && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var certificate = new SupplierCertificate
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                CertificateType = dto.CertificateType.Trim().ToUpper(),
                Name = dto.Name.Trim(),
                Number = dto.Number?.Trim(),
                IssuingAuthority = dto.IssuingAuthority?.Trim(),
                IssueDate = dto.IssueDate,
                ExpiryDate = dto.ExpiryDate,
                Notes = dto.Notes?.Trim(),
                AlertBeforeExpiry = dto.AlertBeforeExpiry ?? true,
                AlertDaysBefore = dto.AlertDaysBefore ?? 30,
                IsActive = true,
                CreatedByEmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupplierCertificates.Add(certificate);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Certificado {CertificateId} adicionado ao fornecedor {SupplierId} por {EmployeeId}",
                certificate.Id, supplierId, employee.Id);

            return CreatedAtAction(nameof(ListCertificates), new { supplierId }, new
            {
                message = "Certificado adicionado com sucesso",
                certificateId = certificate.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar certificado ao fornecedor {SupplierId}", supplierId);
            return StatusCode(500, new { error = "Erro ao adicionar certificado" });
        }
    }

    /// <summary>
    /// Remove certificado do fornecedor
    /// </summary>
    [HttpDelete("{supplierId}/certificates/{certificateId}")]
    public async Task<IActionResult> RemoveCertificate(Guid supplierId, Guid certificateId)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var certificate = await _db.SupplierCertificates
                .Where(c => c.Id == certificateId && c.SupplierId == supplierId && c.IsActive)
                .FirstOrDefaultAsync();

            if (certificate == null)
                return NotFound(new { error = "Certificado não encontrado" });

            certificate.IsActive = false;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Certificado {CertificateId} removido do fornecedor {SupplierId} por {EmployeeId}",
                certificateId, supplierId, employee.Id);

            return Ok(new { message = "Certificado removido com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover certificado {CertificateId}", certificateId);
            return StatusCode(500, new { error = "Erro ao remover certificado" });
        }
    }

    // ==================== EVALUATIONS ====================

    /// <summary>
    /// Lista avaliações do fornecedor
    /// </summary>
    [HttpGet("{supplierId}/evaluations")]
    public async Task<IActionResult> ListEvaluations(Guid supplierId)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == supplierId && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var evaluations = await _db.SupplierEvaluations
                .Where(e => e.SupplierId == supplierId)
                .OrderByDescending(e => e.EvaluationDate)
                .Select(e => new
                {
                    e.Id,
                    e.EvaluationDate,
                    e.Period,
                    e.OverallScore,
                    Scores = new
                    {
                        e.QualityScore,
                        e.DeliveryScore,
                        e.PriceScore,
                        e.ServiceScore,
                        e.DocumentationScore,
                        e.ComplianceScore
                    },
                    Statistics = new
                    {
                        e.TotalOrders,
                        e.OnTimeDeliveries,
                        e.LateDeliveries,
                        e.NonConformities,
                        e.Returns
                    },
                    e.Strengths,
                    e.Weaknesses,
                    e.CorrectiveActions,
                    e.Comments,
                    e.Recommendation,
                    e.IsApproved,
                    e.CreatedAt
                })
                .ToListAsync();

            return Ok(new { data = evaluations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar avaliações do fornecedor {SupplierId}", supplierId);
            return StatusCode(500, new { error = "Erro ao listar avaliações" });
        }
    }

    /// <summary>
    /// Cria avaliação do fornecedor
    /// </summary>
    [HttpPost("{supplierId}/evaluations")]
    public async Task<IActionResult> CreateEvaluation(Guid supplierId, [FromBody] CreateEvaluationDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == supplierId && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            // Calcular pontuação geral
            var scores = new[] {
                dto.QualityScore, dto.DeliveryScore, dto.PriceScore,
                dto.ServiceScore, dto.DocumentationScore, dto.ComplianceScore
            }.Where(s => s.HasValue).ToList();

            var overallScore = scores.Any() ? scores.Average() : 0;

            var evaluation = new SupplierEvaluation
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                EvaluationDate = dto.EvaluationDate ?? DateTime.UtcNow,
                Period = dto.Period?.Trim(),
                QualityScore = dto.QualityScore.HasValue ? (decimal?)dto.QualityScore.Value : null,
                DeliveryScore = dto.DeliveryScore.HasValue ? (decimal?)dto.DeliveryScore.Value : null,
                PriceScore = dto.PriceScore.HasValue ? (decimal?)dto.PriceScore.Value : null,
                ServiceScore = dto.ServiceScore.HasValue ? (decimal?)dto.ServiceScore.Value : null,
                DocumentationScore = dto.DocumentationScore.HasValue ? (decimal?)dto.DocumentationScore.Value : null,
                ComplianceScore = dto.ComplianceScore.HasValue ? (decimal?)dto.ComplianceScore.Value : null,
                OverallScore = (decimal)overallScore,
                TotalOrders = dto.TotalOrders ?? 0,
                OnTimeDeliveries = dto.OnTimeDeliveries ?? 0,
                LateDeliveries = dto.LateDeliveries ?? 0,
                NonConformities = dto.NonConformities ?? 0,
                Returns = dto.Returns ?? 0,
                Strengths = dto.Strengths?.Trim(),
                Weaknesses = dto.Weaknesses?.Trim(),
                CorrectiveActions = dto.CorrectiveActions?.Trim(),
                Comments = dto.Comments?.Trim(),
                Recommendation = dto.Recommendation?.Trim()?.ToUpper(),
                IsApproved = dto.IsApproved ?? true,
                EvaluatedByEmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupplierEvaluations.Add(evaluation);

            // Atualizar dados do fornecedor
            supplier.LastEvaluationDate = evaluation.EvaluationDate;
            supplier.Rating = overallScore;
            supplier.UpdatedByEmployeeId = employee.Id;
            supplier.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Avaliação {EvaluationId} criada para fornecedor {SupplierId} por {EmployeeId}. Pontuação: {Score}",
                evaluation.Id, supplierId, employee.Id, overallScore);

            return CreatedAtAction(nameof(ListEvaluations), new { supplierId }, new
            {
                message = "Avaliação criada com sucesso",
                evaluationId = evaluation.Id,
                overallScore
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar avaliação para fornecedor {SupplierId}", supplierId);
            return StatusCode(500, new { error = "Erro ao criar avaliação" });
        }
    }

    // ==================== HELPER METHODS ====================

    private bool IsEmployeeActive(Employee employee)
    {
        return employee.Status.Equals("Ativo", StringComparison.OrdinalIgnoreCase) &&
               employee.Establishment != null &&
               employee.Establishment.IsActive;
    }

    private bool HasSupplierManagementPermission(Employee employee)
    {
        if (employee.JobPosition == null)
            return false;

        var code = employee.JobPosition.Code.ToLower();

        return code == "owner" ||
               code == "manager" ||
               code == "supervisor" ||
               code == "pharmacist_rt";  // ✅ ADICIONAR
    }

    private bool HasSupplierQualificationPermission(Employee employee)
    {
        if (employee.JobPosition == null)
            return false;

        var code = employee.JobPosition.Code.ToLower();

        return code == "owner" ||
               code == "manager" ||
               code == "supervisor" ||
               code == "pharmacist_rt";  // ✅ ADICIONAR
    }

    private static string RemoveFormatting(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return new string(value.Where(char.IsDigit).ToArray());
    }
}

// ==================== DTOs ====================

public class CreateSupplierDto
{
    [Required(ErrorMessage = "Razão social é obrigatória")]
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

    // ✅ MELHORIA: Novos campos no CreateSupplierDto
    [MaxLength(1)]
    public string? Classification { get; set; }  // A, B, C, D

    [Range(0, 10)]
    public decimal? Rating { get; set; }

    public bool IsQualified { get; set; }

    public int? AverageDeliveryTime { get; set; }

    public int? PaymentTermDays { get; set; }

    public decimal? MinimumOrderValue { get; set; }

    public string? ProductTypes { get; set; }

    public bool HasGmpCertificate { get; set; }

    public bool HasIsoCertificate { get; set; }

    public bool HasAnvisaAuthorization { get; set; }
}

public class UpdateSupplierDto
{
    [MaxLength(200)]
    public string? CompanyName { get; set; }

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

    // ✅ MELHORIA: Novos campos no UpdateSupplierDto
    [MaxLength(1)]
    public string? Classification { get; set; }

    [Range(0, 10)]
    public decimal? Rating { get; set; }

    public bool? IsQualified { get; set; }

    public bool? IsPreferred { get; set; }

    public int? AverageDeliveryTime { get; set; }

    public int? PaymentTermDays { get; set; }

    public decimal? MinimumOrderValue { get; set; }

    public string? ProductTypes { get; set; }

    public bool? HasGmpCertificate { get; set; }

    public bool? HasIsoCertificate { get; set; }

    public bool? HasAnvisaAuthorization { get; set; }
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