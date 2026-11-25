using LabData.Data;
using LabData.Repositories.Interfaces;
using LabIdentity.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabIdentity.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        const int pageSize = 5;

        var totalCount = await _userRepository.CountAsync();

        var pagination = new PaginationViewModel(
            totalCount,
            page,
            pageSize);

        var users = await _userRepository.GetPageAsync(
            pagination.PageNumber,
            pagination.PageSize);

        var model = new PagedListViewModel<ApplicationUser>
        {
            Items = users,
            Pagination = pagination
        };

        return View(model);
    }
}