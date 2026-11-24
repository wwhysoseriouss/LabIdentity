using System.Diagnostics;
using LabData.Models;
using LabIdentity.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;

namespace LabIdentity.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly LabSettings _settings;
    private readonly IStringLocalizer<HomeController> _localizer;

    public HomeController(
    ILogger<HomeController> logger,
    LabSettings settings,
    IStringLocalizer<HomeController> localizer)
    {
        _logger = logger;
        _settings = settings;
        _localizer = localizer;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        ViewData["FooterApplicationName"] = _settings.App.ApplicationName;
        ViewData["FooterMaxOptionsPerPoll"] = _settings.Polling.MaxOptionsPerPoll;

        base.OnActionExecuting(context);
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        ViewData["WelcomeMessage"] = _localizer["WelcomeMessage"].Value;
        ViewData["PollsDescription"] = _localizer["PollsDescription"].Value;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize(Policy = "VerifiedClient")]
    public IActionResult Archive()
    {
        return View();
    }
    
    [Authorize(Policy = "PremiumAccess")]
    public IActionResult Premium()
    {
        return View();
    }
    
    [Authorize(Policy = "ForumAccess")]
    public IActionResult Forum()
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