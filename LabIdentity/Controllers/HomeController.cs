using System.Diagnostics;
using LabData.Models;
using LabIdentity.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LabIdentity.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly LabSettings _settings;

    public HomeController(
        ILogger<HomeController> logger,
        LabSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["FooterApplicationName"] = _settings.App.ApplicationName;
        ViewData["FooterMaxOptionsPerPoll"] = _settings.Polling.MaxOptionsPerPoll;

        base.OnActionExecuting(context);
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}