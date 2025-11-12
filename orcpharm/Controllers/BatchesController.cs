using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;
using Models.DTOs;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class BatchesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<BatchesController> _logger;

    public BatchesController(AppDbContext context, ILogger<BatchesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<BatchListResponse>> GetBatches(
        [FromQuery] Guid? rawMaterialId,
        [FromQuery] Guid? supplierId,
        [FromQuery] string? status,
        [FromQuery] bool? isExpired,
        [FromQuery] bool? nearExpiration,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var query = _context.Batches
                .Include(b => b.RawMaterial)
                .Include(b => b.Supplier)
                .AsQueryable();

            if (rawMaterialId.HasValue)
                query = query.Where(b => b.RawMaterialId == rawMaterialId.Value);

            if (supplierId.HasValue)
                query = query.Where(b => b.SupplierId == supplierId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(b => b.Status == status.ToUpper());

            if (isExpired.HasValue && isExpired.Value)
                query = query.Where(b => b.ExpiryDate < DateTime.UtcNow);

            if (nearExpiration.HasValue && nearExpiration.Value)
            {
                var thresholdDate = DateTime.UtcNow.AddDays(90);
                query = query.Where(b => b.ExpiryDate <= thresholdDate && b.ExpiryDate >= DateTime.UtcNow);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(b =>
                    b.BatchNumber.ToLower().Contains(searchTerm) ||
                    b.InvoiceNumber.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var batches = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BatchResponse
                {
                    Id = b.Id,
                    BatchNumber = b.BatchNumber,
                    RawMaterialId = b.RawMaterialId,
                    RawMaterialName = b.RawMaterial != null ? b.RawMaterial.Name : null,
                    SupplierId = b.SupplierId,
                    SupplierName = b.Supplier != null ? b.Supplier.TradeName : null,
                    InvoiceNumber = b.InvoiceNumber,
                    ReceivedQuantity = b.ReceivedQuantity,
                    CurrentQuantity = b.CurrentQuantity,
                    UnitCost = b.UnitCost,
                    ReceivedDate = b.ReceivedDate,
                    ExpiryDate = b.ExpiryDate,
                    ManufactureDate = b.ManufactureDate,
                    Status = b.Status,
                    CertificateNumber = b.CertificateNumber,
                    ApprovalDate = b.ApprovalDate,
                    ApprovedByEmployeeId = b.ApprovedByEmployeeId,
                    QualityNotes = b.QualityNotes,
                    CreatedAt = b.CreatedAt,
                    CreatedByEmployeeId = b.CreatedByEmployeeId,
                    DaysUntilExpiry = (b.ExpiryDate.Date - DateTime.UtcNow.Date).Days,
                    IsExpired = DateTime.UtcNow.Date >= b.ExpiryDate.Date,
                    IsNearExpiration = (b.ExpiryDate.Date - DateTime.UtcNow.Date).Days <= 90 && (b.ExpiryDate.Date - DateTime.UtcNow.Date).Days > 0,
                    UsagePercentage = b.ReceivedQuantity > 0 ? ((b.ReceivedQuantity - b.CurrentQuantity) / b.ReceivedQuantity) * 100 : 0
                })
                .ToListAsync();

            return Ok(new BatchListResponse
            {
                Batches = batches,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches");
            return StatusCode(500, new { message = "Error retrieving batches", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BatchResponse>> GetBatch(Guid id)
    {
        try
        {
            var batch = await _context.Batches
                .Include(b => b.RawMaterial)
                .Include(b => b.Supplier)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            return Ok(new BatchResponse
            {
                Id = batch.Id,
                BatchNumber = batch.BatchNumber,
                RawMaterialId = batch.RawMaterialId,
                RawMaterialName = batch.RawMaterial?.Name,
                SupplierId = batch.SupplierId,
                SupplierName = batch.Supplier?.TradeName,
                InvoiceNumber = batch.InvoiceNumber,
                ReceivedQuantity = batch.ReceivedQuantity,
                CurrentQuantity = batch.CurrentQuantity,
                UnitCost = batch.UnitCost,
                ReceivedDate = batch.ReceivedDate,
                ExpiryDate = batch.ExpiryDate,
                ManufactureDate = batch.ManufactureDate,
                Status = batch.Status,
                CertificateNumber = batch.CertificateNumber,
                ApprovalDate = batch.ApprovalDate,
                ApprovedByEmployeeId = batch.ApprovedByEmployeeId,
                QualityNotes = batch.QualityNotes,
                CreatedAt = batch.CreatedAt,
                CreatedByEmployeeId = batch.CreatedByEmployeeId,
                DaysUntilExpiry = (batch.ExpiryDate.Date - DateTime.UtcNow.Date).Days,
                IsExpired = DateTime.UtcNow.Date >= batch.ExpiryDate.Date,
                IsNearExpiration = (batch.ExpiryDate.Date - DateTime.UtcNow.Date).Days <= 90,
                UsagePercentage = batch.ReceivedQuantity > 0 ? ((batch.ReceivedQuantity - batch.CurrentQuantity) / batch.ReceivedQuantity) * 100 : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch {BatchId}", id);
            return StatusCode(500, new { message = "Error retrieving batch", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<BatchResponse>> CreateBatch(
        [FromBody] CreateBatchRequest request,
        [FromQuery] Guid userId)
    {
        try
        {
            var rawMaterial = await _context.RawMaterials.FindAsync(request.RawMaterialId);
            if (rawMaterial == null)
                return BadRequest(new { message = "Raw material not found" });

            var supplier = await _context.Suppliers.FindAsync(request.SupplierId);
            if (supplier == null)
                return BadRequest(new { message = "Supplier not found" });

            if (request.ManufactureDate.HasValue && request.ExpiryDate <= request.ManufactureDate.Value)
                return BadRequest(new { message = "Expiry date must be after manufacture date" });

            var existingBatch = await _context.Batches
                .AnyAsync(b => b.BatchNumber == request.BatchNumber && b.RawMaterialId == request.RawMaterialId);

            if (existingBatch)
                return BadRequest(new { message = "Batch number already exists for this raw material" });

            var batch = new Batch
            {
                Id = Guid.NewGuid(),
                RawMaterialId = request.RawMaterialId,
                SupplierId = request.SupplierId,
                BatchNumber = request.BatchNumber,
                InvoiceNumber = request.InvoiceNumber,
                ReceivedQuantity = request.ReceivedQuantity,
                CurrentQuantity = request.ReceivedQuantity,
                UnitCost = request.UnitCost,
                ReceivedDate = request.ReceivedDate ?? DateTime.UtcNow,
                ExpiryDate = request.ExpiryDate,
                ManufactureDate = request.ManufactureDate,
                Status = "QUARENTENA",
                CertificateNumber = request.CertificateNumber,
                QualityNotes = request.QualityNotes,
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = userId
            };

            _context.Batches.Add(batch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchNumber} created by user {UserId}", batch.BatchNumber, userId);

            return CreatedAtAction(nameof(GetBatch), new { id = batch.Id }, new BatchResponse
            {
                Id = batch.Id,
                BatchNumber = batch.BatchNumber,
                Status = batch.Status,
                CreatedAt = batch.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating batch");
            return StatusCode(500, new { message = "Error creating batch", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBatch(
        Guid id,
        [FromBody] UpdateBatchRequest request)
    {
        try
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            if (request.CurrentQuantity.HasValue)
            {
                if (request.CurrentQuantity.Value < 0)
                    return BadRequest(new { message = "Current quantity cannot be negative" });
                if (request.CurrentQuantity.Value > batch.ReceivedQuantity)
                    return BadRequest(new { message = "Current quantity cannot exceed received quantity" });
                batch.CurrentQuantity = request.CurrentQuantity.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var validStatuses = new[] { "QUARENTENA", "APROVADO", "REPROVADO", "VENCIDO" };
                if (!validStatuses.Contains(request.Status.ToUpper()))
                    return BadRequest(new { message = "Invalid status" });
                batch.Status = request.Status.ToUpper();
            }

            if (request.ApprovalDate.HasValue)
                batch.ApprovalDate = request.ApprovalDate;

            if (request.ApprovedByEmployeeId.HasValue)
                batch.ApprovedByEmployeeId = request.ApprovedByEmployeeId;

            if (request.QualityNotes != null)
                batch.QualityNotes = request.QualityNotes;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} updated", id);

            return Ok(new { message = "Batch updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating batch {BatchId}", id);
            return StatusCode(500, new { message = "Error updating batch", error = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveBatch(
        Guid id,
        [FromBody] ApproveBatchRequest request,
        [FromQuery] Guid userId)
    {
        try
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            if (batch.Status != "QUARENTENA")
                return BadRequest(new { message = "Only batches in quarantine can be approved" });

            batch.Status = "APROVADO";
            batch.ApprovedByEmployeeId = userId;
            batch.ApprovalDate = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Notes))
                batch.QualityNotes = string.IsNullOrWhiteSpace(batch.QualityNotes)
                    ? request.Notes
                    : $"{batch.QualityNotes}\n[APROVAÇÃO] {request.Notes}";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} approved by user {UserId}", id, userId);

            return Ok(new { message = "Batch approved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving batch {BatchId}", id);
            return StatusCode(500, new { message = "Error approving batch", error = ex.Message });
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectBatch(
        Guid id,
        [FromBody] RejectBatchRequest request,
        [FromQuery] Guid userId)
    {
        try
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            if (batch.Status != "QUARENTENA")
                return BadRequest(new { message = "Only batches in quarantine can be rejected" });

            batch.Status = "REPROVADO";
            batch.ApprovedByEmployeeId = userId;
            batch.ApprovalDate = DateTime.UtcNow;
            batch.QualityNotes = string.IsNullOrWhiteSpace(batch.QualityNotes)
                ? $"[REPROVADO] {request.RejectionReason}"
                : $"{batch.QualityNotes}\n[REPROVADO] {request.RejectionReason}";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} rejected by user {UserId}", id, userId);

            return Ok(new { message = "Batch rejected successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting batch {BatchId}", id);
            return StatusCode(500, new { message = "Error rejecting batch", error = ex.Message });
        }
    }

    [HttpPost("{id}/adjust-quantity")]
    public async Task<IActionResult> AdjustBatchQuantity(
        Guid id,
        [FromBody] AdjustBatchQuantityRequest request)
    {
        try
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            var newQuantity = batch.CurrentQuantity + request.QuantityAdjustment;

            if (newQuantity < 0)
                return BadRequest(new { message = "Resulting quantity cannot be negative" });

            if (newQuantity > batch.ReceivedQuantity)
                return BadRequest(new { message = "Resulting quantity cannot exceed received quantity" });

            batch.CurrentQuantity = newQuantity;

            var adjustmentNote = $"[AJUSTE] {request.QuantityAdjustment:+0.####;-0.####} - {request.Reason}";
            batch.QualityNotes = string.IsNullOrWhiteSpace(batch.QualityNotes)
                ? adjustmentNote
                : $"{batch.QualityNotes}\n{adjustmentNote}";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} quantity adjusted by {Adjustment}", id, request.QuantityAdjustment);

            return Ok(new
            {
                message = "Quantity adjusted successfully",
                previousQuantity = batch.CurrentQuantity - request.QuantityAdjustment,
                currentQuantity = batch.CurrentQuantity,
                adjustment = request.QuantityAdjustment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting batch {BatchId} quantity", id);
            return StatusCode(500, new { message = "Error adjusting quantity", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBatch(Guid id)
    {
        try
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            if (batch.CurrentQuantity < batch.ReceivedQuantity)
                return BadRequest(new { message = "Cannot delete batch with usage history" });

            _context.Batches.Remove(batch);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Batch {BatchId} deleted", id);

            return Ok(new { message = "Batch deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting batch {BatchId}", id);
            return StatusCode(500, new { message = "Error deleting batch", error = ex.Message });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<BatchStatsResponse>> GetBatchStats()
    {
        try
        {
            var query = _context.Batches;

            var totalBatches = await query.CountAsync();
            var quarantineBatches = await query.CountAsync(b => b.Status == "QUARENTENA");
            var approvedBatches = await query.CountAsync(b => b.Status == "APROVADO");
            var rejectedBatches = await query.CountAsync(b => b.Status == "REPROVADO");
            var expiredBatches = await query.CountAsync(b => b.ExpiryDate < DateTime.UtcNow);

            var nearExpirationDate = DateTime.UtcNow.AddDays(90);
            var nearExpirationBatches = await query.CountAsync(b =>
                b.ExpiryDate <= nearExpirationDate && b.ExpiryDate >= DateTime.UtcNow);

            var totalInventoryValue = await query
                .Where(b => b.Status == "APROVADO")
                .SumAsync(b => b.CurrentQuantity * b.UnitCost);

            var batchesByStatus = await query
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            return Ok(new BatchStatsResponse
            {
                TotalBatches = totalBatches,
                QuarantineBatches = quarantineBatches,
                ApprovedBatches = approvedBatches,
                RejectedBatches = rejectedBatches,
                ExpiredBatches = expiredBatches,
                NearExpirationBatches = nearExpirationBatches,
                TotalInventoryValue = totalInventoryValue,
                BatchesByStatus = batchesByStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch statistics");
            return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
        }
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<List<BatchResponse>>> GetExpiringBatches([FromQuery] int days = 90)
    {
        try
        {
            var thresholdDate = DateTime.UtcNow.AddDays(days);

            var batches = await _context.Batches
                .Include(b => b.RawMaterial)
                .Where(b => b.Status == "APROVADO" &&
                           b.ExpiryDate <= thresholdDate &&
                           b.ExpiryDate >= DateTime.UtcNow &&
                           b.CurrentQuantity > 0)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new BatchResponse
                {
                    Id = b.Id,
                    BatchNumber = b.BatchNumber,
                    RawMaterialName = b.RawMaterial != null ? b.RawMaterial.Name : null,
                    ExpiryDate = b.ExpiryDate,
                    CurrentQuantity = b.CurrentQuantity,
                    DaysUntilExpiry = (b.ExpiryDate.Date - DateTime.UtcNow.Date).Days
                })
                .ToListAsync();

            return Ok(batches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring batches");
            return StatusCode(500, new { message = "Error retrieving expiring batches", error = ex.Message });
        }
    }

    [HttpGet("by-raw-material/{rawMaterialId}")]
    public async Task<ActionResult<List<BatchResponse>>> GetBatchesByRawMaterial(
        Guid rawMaterialId,
        [FromQuery] bool onlyAvailable = false)
    {
        try
        {
            var query = _context.Batches
                .Include(b => b.RawMaterial)
                .Where(b => b.RawMaterialId == rawMaterialId);

            if (onlyAvailable)
            {
                query = query.Where(b => b.Status == "APROVADO" &&
                                        b.ExpiryDate > DateTime.UtcNow &&
                                        b.CurrentQuantity > 0);
            }

            var batches = await query
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new BatchResponse
                {
                    Id = b.Id,
                    BatchNumber = b.BatchNumber,
                    ExpiryDate = b.ExpiryDate,
                    CurrentQuantity = b.CurrentQuantity,
                    Status = b.Status,
                    DaysUntilExpiry = (b.ExpiryDate.Date - DateTime.UtcNow.Date).Days
                })
                .ToListAsync();

            return Ok(batches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batches for raw material {RawMaterialId}", rawMaterialId);
            return StatusCode(500, new { message = "Error retrieving batches", error = ex.Message });
        }
    }
}