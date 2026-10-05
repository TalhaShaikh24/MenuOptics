using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;

namespace MenuOptics.Controllers
{
    public class CategoryController : Controller
    {
        [AuthorizeRestaurant]
        public IActionResult Index()
        {
            return View();
        }
    }
}
