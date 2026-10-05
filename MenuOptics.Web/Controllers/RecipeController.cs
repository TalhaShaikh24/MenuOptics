using MenuOptics.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MenuOptics.Controllers
{
    public class RecipeController : Controller
    {
        [AuthorizeRestaurant]
        public IActionResult Index()
        {
            return View();
        }
    }
}
