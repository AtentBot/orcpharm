using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controllers;


[Route("PDV")]
public class PDVViewController : Controller
{
    // GET: /PDV
    [HttpGet("")]
    public IActionResult Index()
    {
        return View("~/Views/PDV/Index.cshtml");
    }
}