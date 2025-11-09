using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace orcpharm.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;

    public SuppliersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (employee == null) return Unauthorized();

        var suppliers = await _db.Suppliers
            .Where(s => s.EstablishmentId == employee.EstablishmentId && s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Cnpj,
                s.Phone,
                s.IsQualified,
                s.AfeNumber,
                s.AfeExpiryDate,
                s.SuppliesControlled,
                AfeStatus = s.AfeExpiryDate < DateTime.Now ? "VENCIDO" : "VÁLIDO"
            })
            .ToListAsync();

        return Ok(suppliers);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (employee == null) return Unauthorized();

        // Verificar CNPJ duplicado
        if (await _db.Suppliers.AnyAsync(s => s.Cnpj == dto.Cnpj))
            return Conflict(new { error = "CNPJ já cadastrado" });

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            EstablishmentId = employee.EstablishmentId,
            Name = dto.Name,
            Cnpj = dto.Cnpj,
            TradeName = dto.TradeName,
            AfeNumber = dto.AfeNumber,
            AfeExpiryDate = dto.AfeExpiryDate,
            Phone = dto.Phone,
            Email = dto.Email,
            Street = dto.Street,
            Number = dto.Number,
            Neighborhood = dto.Neighborhood,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            SuppliesControlled = dto.SuppliesControlled,
            SuppliesAntibiotics = dto.SuppliesAntibiotics,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employee.Id
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return Ok(supplier);
    }

    public class CreateSupplierDto
    {
        public string Name { get; set; } = "";
        public string Cnpj { get; set; } = "";
        public string? TradeName { get; set; }
        public string? AfeNumber { get; set; }
        public DateTime? AfeExpiryDate { get; set; }
        public string Phone { get; set; } = "";
        public string? Email { get; set; }
        public string Street { get; set; } = "";
        public string Number { get; set; } = "";
        public string Neighborhood { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public bool SuppliesControlled { get; set; }
        public bool SuppliesAntibiotics { get; set; }
    }
}