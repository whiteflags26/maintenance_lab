using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace SpenditWeb.Controllers
{
    public abstract class SpenditControllerBase : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        protected SpenditControllerBase(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        protected string CurrentUserId => _userManager.GetUserId(User)
            ?? throw new InvalidOperationException("The current user does not have an ID.");
    }
}
