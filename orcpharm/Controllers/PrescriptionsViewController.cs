using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;

namespace Controllers
{
    /// <summary>
    /// MVC Controller para Views de Prescrições
    /// Separado do API Controller para manter responsabilidades distintas
    /// </summary>

    [Route("PrescriptionsView")]
    public class PrescriptionsViewController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PrescriptionsViewController> _logger;

        public PrescriptionsViewController(
            AppDbContext context,
            ILogger<PrescriptionsViewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ====================================================================
        // LISTA DE PRESCRIÇÕES
        // ====================================================================

        /// <summary>
        /// GET: /PrescriptionsView
        /// Lista todas as prescrições
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 20)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();

                // Se claims não existem, o [Authorize] já garantiu autenticação
                // Então deve ser erro de configuração do middleware
                if (establishmentId == Guid.Empty)
                {
                    _logger.LogError("EstablishmentId claim is missing for authenticated user");
                    TempData["Error"] = "Erro de configuração: estabelecimento não identificado";
                    return View(new List<object>());
                }

                var query = _context.Prescriptions
                    .Where(p => p.EstablishmentId == establishmentId)
                    .AsQueryable();

                // Filtro por status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                // Paginação
                var total = await query.CountAsync();
                var prescriptions = await query
                    .OrderByDescending(p => p.PrescriptionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.CustomerId,
                        CustomerName = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.FullName)
                            .FirstOrDefault() ?? "N/A",
                        p.PrescriptionDate,
                        p.ExpirationDate,
                        p.DoctorName,
                        p.Status,
                        p.PrescriptionType
                    })
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
                ViewBag.Status = status;
                ViewBag.TotalRecords = total;

                return View(prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar prescrições");
                TempData["Error"] = "Erro ao carregar prescrições";
                return View(new List<object>());
            }
        }

        // ====================================================================
        // DETALHES DA PRESCRIÇÃO
        // ====================================================================

        /// <summary>
        /// GET: /PrescriptionsView/Details/{id}
        /// Exibe detalhes de uma prescrição
        /// </summary>
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var prescription = await _context.Prescriptions
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.CustomerId,
                        CustomerName = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.FullName)
                            .FirstOrDefault(),
                        CustomerCpf = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.Cpf)
                            .FirstOrDefault(),
                        p.PrescriptionDate,
                        p.ExpirationDate,
                        p.DoctorName,
                        p.DoctorCrm,
                        p.DoctorCrmState,
                        p.Status,
                        p.PrescriptionType,
                        p.ControlledType,
                        p.PrescriptionColor,
                        p.Medications,
                        p.Posology,
                        p.Observations,
                        p.ImageUrl,
                        p.ValidatedAt,
                        ValidatedByName = _context.Employees
                            .Where(e => e.Id == p.ValidatedByEmployeeId)
                            .Select(e => e.FullName)
                            .FirstOrDefault(),
                        p.ValidationNotes,
                        p.ManipulationOrderId,
                        p.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Buscar arquivos OCR relacionados
                var files = await _context.Set<PrescriptionFile>()
                    .Where(f => f.PrescriptionId == id)
                    .OrderByDescending(f => f.UploadedAt)
                    .Select(f => new
                    {
                        f.Id,
                        f.FileName,
                        f.FileType,
                        f.UploadedAt,
                        f.OcrStatus,
                        f.OcrConfidence,
                        f.OcrProcessedAt
                    })
                    .ToListAsync();

                ViewBag.Files = files;

                return View(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar detalhes da prescrição {Id}", id);
                TempData["Error"] = "Erro ao carregar detalhes";
                return RedirectToAction(nameof(Index));
            }
        }

        // ====================================================================
        // PÁGINA DE UPLOAD OCR
        // ====================================================================

        /// <summary>
        /// GET: /PrescriptionsView/OcrUpload/{id}
        /// Página dedicada para upload e processamento OCR
        /// </summary>
        [HttpGet("OcrUpload/{id}")]
        public async Task<IActionResult> OcrUpload(Guid id)
        {
            try
            {
                var prescription = await _context.Prescriptions
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Status,
                        CustomerName = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.FullName)
                            .FirstOrDefault(),
                        p.PrescriptionDate
                    })
                    .FirstOrDefaultAsync();

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                return View(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar página OCR para prescrição {Id}", id);
                TempData["Error"] = "Erro ao carregar página";
                return RedirectToAction(nameof(Index));
            }
        }

        // ====================================================================
        // CRIAR NOVA PRESCRIÇÃO (FORMULÁRIO)
        // ====================================================================

        /// <summary>
        /// GET: /PrescriptionsView/Create
        /// Exibe formulário para criar nova prescrição
        /// </summary>
        [HttpGet("Create")]
        public IActionResult Create()
        {
            // Carregar dados para dropdowns
            LoadCustomersForDropdown();
            return View();
        }

        /// <summary>
        /// POST: /PrescriptionsView/Create
        /// Processa criação de nova prescrição
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prescription model)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();

                model.Id = Guid.NewGuid();
                model.EstablishmentId = establishmentId;
                model.CreatedByEmployeeId = employeeId;
                model.Status = "PENDENTE";

                // Gerar código automático
                model.Code = await GeneratePrescriptionCode();

                _context.Prescriptions.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Prescrição criada com sucesso!";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar prescrição");
                TempData["Error"] = "Erro ao criar prescrição";
                LoadCustomersForDropdown();
                return View(model);
            }
        }

        // ====================================================================
        // VALIDAR PRESCRIÇÃO (FARMACÊUTICO)
        // ====================================================================

        /// <summary>
        /// GET: /PrescriptionsView/Validate/{id}
        /// Formulário para validação farmacêutica
        /// </summary>
        [HttpGet("Validate/{id}")]
        public async Task<IActionResult> Validate(Guid id)
        {
            try
            {
                var prescription = await _context.Prescriptions.FindAsync(id);

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (prescription.Status != "PENDENTE")
                {
                    TempData["Warning"] = "Esta prescrição já foi validada";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View(prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar validação {Id}", id);
                TempData["Error"] = "Erro ao carregar validação";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /PrescriptionsView/Validate/{id}
        /// Processa validação da prescrição
        /// </summary>
        [HttpPost("Validate/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Validate(Guid id, string validationNotes)
        {
            try
            {
                var employeeId = GetEmployeeId();

                if (!HasPermission("FARMACEUTICO"))
                {
                    TempData["Error"] = "Apenas farmacêuticos podem validar prescrições";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var prescription = await _context.Prescriptions.FindAsync(id);

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                prescription.Status = "VALIDADA";
                prescription.ValidatedAt = DateTime.UtcNow;
                prescription.ValidatedByEmployeeId = employeeId;
                prescription.ValidationNotes = validationNotes;
                prescription.UpdatedByEmployeeId = employeeId;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Prescrição validada com sucesso!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar prescrição {Id}", id);
                TempData["Error"] = "Erro ao validar prescrição";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ====================================================================
        // MÉTODOS AUXILIARES
        // ====================================================================

        private Guid GetEmployeeId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
            if (claim == null || !Guid.TryParse(claim.Value, out var id))
            {
                _logger.LogWarning("EmployeeId claim not found or invalid");
                return Guid.Empty;
            }
            return id;
        }

        private Guid GetEstablishmentId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "EstablishmentId");
            if (claim == null || !Guid.TryParse(claim.Value, out var id))
            {
                _logger.LogWarning("EstablishmentId claim not found or invalid");
                return Guid.Empty;
            }
            return id;
        }

        private bool HasPermission(string jobPositionCode)
        {
            var positionClaim = User.Claims.FirstOrDefault(c => c.Type == "JobPositionCode");
            return positionClaim?.Value == jobPositionCode;
        }

        private void LoadCustomersForDropdown()
        {
            var establishmentId = GetEstablishmentId();
            ViewBag.Customers = _context.Customers
                .Where(c => c.EstablishmentId == establishmentId)
                .OrderBy(c => c.FullName)
                .Select(c => new { c.Id, c.FullName, c.Cpf })
                .ToList();
        }

        private async Task<string> GeneratePrescriptionCode()
        {
            var year = DateTime.Now.Year;
            var lastCode = await _context.Prescriptions
                .Where(p => p.Code.StartsWith($"RX{year}"))
                .OrderByDescending(p => p.Code)
                .Select(p => p.Code)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastCode != null && int.TryParse(lastCode.Substring(6), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }

            return $"RX{year}{nextNumber:D6}";
        }
    }
}