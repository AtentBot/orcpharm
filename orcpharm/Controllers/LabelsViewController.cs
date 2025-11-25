using Microsoft.AspNetCore.Mvc;
using DTOs.Labels;

namespace Controllers;

[Route("Labels")]
public class LabelsViewController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public LabelsViewController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;

        var baseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:8080";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/Labels/templates");
            if (response.IsSuccessStatusCode)
            {
                var templates = await response.Content.ReadFromJsonAsync<List<LabelTemplateResponseDto>>();
                return View(templates ?? new List<LabelTemplateResponseDto>());
            }

            return View(new List<LabelTemplateResponseDto>());
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Erro ao carregar templates: {ex.Message}";
            return View(new List<LabelTemplateResponseDto>());
        }
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new CreateLabelTemplateDto());
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/Labels/templates/{id}");
            if (response.IsSuccessStatusCode)
            {
                var template = await response.Content.ReadFromJsonAsync<LabelTemplateResponseDto>();
                var dto = new UpdateLabelTemplateDto
                {
                    Name = template!.Name,
                    Description = template.Description,
                    HtmlTemplate = template.HtmlTemplate,
                    CssStyles = template.CssStyles,
                    IsActive = template.IsActive,
                    IsDefault = template.IsDefault
                };

                ViewBag.TemplateId = id;
                return View(dto);
            }

            TempData["Error"] = "Template não encontrado";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Erro ao carregar template: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet("Generate/{manipulationOrderId}")]
    public IActionResult Generate(Guid manipulationOrderId)
    {
        ViewBag.ManipulationOrderId = manipulationOrderId;
        return View();
    }

    [HttpGet("View/{labelId}")]
    public IActionResult View(Guid labelId)
    {
        ViewBag.LabelId = labelId;
        return View();
    }

    [HttpGet("History")]
    public IActionResult History()
    {
        return View();
    }

    [HttpGet("Search")]
    public IActionResult Search()
    {
        return View();
    }
}