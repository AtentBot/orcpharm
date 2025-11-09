using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockMovementsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StockMovementsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("entrada")]
    public async Task<IActionResult> EntradaEstoque([FromBody] EntradaDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (employee == null) return Unauthorized();

        var material = await _db.RawMaterials
            .FirstOrDefaultAsync(rm => rm.Id == dto.RawMaterialId);

        if (material == null) return NotFound("Material não encontrado");

        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // Criar lote
            var batch = new Batch
            {
                Id = Guid.NewGuid(),
                RawMaterialId = dto.RawMaterialId,
                SupplierId = dto.SupplierId,
                BatchNumber = dto.BatchNumber,
                InvoiceNumber = dto.InvoiceNumber,
                ReceivedQuantity = dto.Quantity,
                CurrentQuantity = dto.Quantity,
                UnitCost = dto.UnitCost,
                ReceivedDate = DateTime.UtcNow,
                ExpiryDate = dto.ExpiryDate,
                Status = "QUARENTENA",
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employee.Id
            };

            _db.Batches.Add(batch);

            // Criar movimentação
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                EstablishmentId = employee.EstablishmentId,
                RawMaterialId = dto.RawMaterialId,
                BatchId = batch.Id,
                MovementType = "ENTRADA",
                Quantity = dto.Quantity,
                StockBefore = material.CurrentStock,
                StockAfter = material.CurrentStock + dto.Quantity,
                DocumentNumber = dto.InvoiceNumber,
                MovementDate = DateTime.UtcNow,
                PerformedByEmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockMovements.Add(movement);

            // Atualizar estoque
            material.CurrentStock += dto.Quantity;
            material.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { batchId = batch.Id, newStock = material.CurrentStock });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public class EntradaDto
    {
        public Guid RawMaterialId { get; set; }
        public Guid SupplierId { get; set; }
        public string BatchNumber { get; set; } = "";
        public string InvoiceNumber { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}