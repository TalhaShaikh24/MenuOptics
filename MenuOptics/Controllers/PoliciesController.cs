using Microsoft.AspNetCore.Mvc;

namespace MenuOptics.Controllers
{
    public class PoliciesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
