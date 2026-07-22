using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;

namespace MenuOptics.Controllers
{
    public class CategoryController : Controller
    {
        [AuthorizeAdmin]
        public IActionResult Index()
        {
            return View();
        }
    }
}
