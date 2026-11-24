using LabData.Data;
using LabIdentity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace LabIdentity.Controllers;

public class ParticipantsController : Controller
{
    private static bool IsParticipationCodeValid(
    string? participationCode,
    int? age)
    {
        if (string.IsNullOrWhiteSpace(participationCode))
        {
            return true;
        }

        var code = participationCode.Trim().ToUpperInvariant();

        return code switch
        {
            "POLL2026" => age is >= 18 and <= 120,
            "YOUTH2026" => age is >= 18 and <= 25,
            _ => false
        };
    }

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<RegisterParticipantViewModel> _localizer;

    public ParticipantsController(
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<RegisterParticipantViewModel> localizer)
    {
        _userManager = userManager;
        _localizer = localizer;
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Json(true);
        }

        var existingUser = await _userManager.FindByEmailAsync(
            email.Trim());

        return Json(existingUser == null);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult VerifyParticipationCode(
    string? participationCode,
    int? age)
    {
        return Json(
            IsParticipationCodeValid(participationCode, age));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterParticipantViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterParticipantViewModel model)
    {
        // Перевіряємо атрибути валідації ViewModel.
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!IsParticipationCodeValid(
        model.ParticipationCode,
        model.Age))
        {
            ModelState.AddModelError(
                nameof(model.ParticipationCode),
                _localizer["InvalidParticipationCode"].Value);

            return View(model);
        }

        var email = model.Email.Trim();

        // Окрема серверна перевірка унікальності email.
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                _localizer["EmailTaken"].Value);

            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim()
        };

        // Identity перевіряє пароль і зберігає його хеш.
        var result = await _userManager.CreateAsync(
            user,
            model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        return RedirectToPage(
            "/Account/RegisterConfirmation",
            new
            {
                area = "Identity",
                email,
                returnUrl = Url.Content("~/")
            });
    }
}