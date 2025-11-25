using Microsoft.AspNetCore.Mvc;

namespace Controllers;

public class WorkflowController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult Kanban()
    {
        return View();
    }
}
