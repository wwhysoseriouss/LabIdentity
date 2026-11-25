using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LabData.Data;
using LabData.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LabIdentity.ViewModels;

namespace LabIdentity.Controllers;

[Authorize]
public class PollDraftsController(
    ApplicationDbContext db,
    IAuthorizationService authorization) : Controller
{
    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new PollDraftFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PollDraftFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        var authorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(authorId))
        {
            return Forbid();
        }

        var draft = new PollDraft
        {
            Question = form.Question.Trim(),
            AuthorId = authorId
        };

        db.PollDrafts.Add(draft);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Edit), new { id = draft.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit([FromRoute] int id)
    {
        var draft = await db.PollDrafts.FindAsync(id);

        if (draft is null)
        {
            return NotFound();
        }

        var access = await authorization.AuthorizeAsync(User, draft, "EditOwnPollDraft");

        if (!access.Succeeded)
        {
            return Forbid();
        }

        return View("Form", new PollDraftFormViewModel { Question = draft.Question });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, PollDraftFormViewModel form)
    {
        var draft = await db.PollDrafts.FindAsync(id);

        if (draft is null)
        {
            return NotFound();
        }

        var access = await authorization.AuthorizeAsync(User, draft, "EditOwnPollDraft");

        if (!access.Succeeded)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        draft.Question = form.Question.Trim();
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Edit), new { id });
    }
}