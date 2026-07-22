using Microsoft.AspNetCore.Mvc;

namespace MenuOptics.Controllers
{
    public class RecipeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
