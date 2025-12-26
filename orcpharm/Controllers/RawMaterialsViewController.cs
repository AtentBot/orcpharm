using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public class RawMaterialsViewController : Controller
{
    [HttpGet("/materias-primas")]
    public IActionResult Index()
    {
        return View("~/Views/RawMaterials/Index.cshtml");
    }

    [HttpGet("/materias-primas/criar")]
    public IActionResult Create()
    {
        return View("~/Views/RawMaterials/Create.cshtml");
    }

    [HttpGet("/materias-primas/{id}")]
    public IActionResult Details(Guid id)
    {
        ViewBag.RawMaterialId = id;
        return View("~/Views/RawMaterials/Details.cshtml");
    }

    [HttpGet("/materias-primas/{id}/editar")]
    public IActionResult Edit(Guid id)
    {
        ViewBag.RawMaterialId = id;
        return View("~/Views/RawMaterials/Edit.cshtml");
    }

    [HttpGet("/materias-primas/{id}/lotes")]
    public IActionResult Batches(Guid id)
    {
        ViewBag.RawMaterialId = id;
        return View("~/Views/RawMaterials/Batches.cshtml");
    }

    /// <summary>
    /// Dashboard de Preços - Fase 4.3
    /// </summary>
    [HttpGet("/materias-primas/precos")]
    public IActionResult Precos()
    {
        return View("~/Views/RawMaterials/Precos.cshtml");
    }
}
