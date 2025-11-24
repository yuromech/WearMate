using Microsoft.AspNetCore.Mvc;
using WearMate.Web.Middleware;
using WearMate.Web.Models.ViewModels;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class UsersController : Controller
{
    public IActionResult Index()
    {
        var viewModel = new UserListViewModel
        {
            Users = new List<UserSummary>(),
            TotalUsers = 0,
            ActiveUsers = 0,
            InactiveUsers = 0
        };

        return View(viewModel);
    }

    public IActionResult Detail(Guid id)
    {
        return View();
    }
}
