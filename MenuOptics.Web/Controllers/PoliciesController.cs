using MenuOptics.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MenuOptics.Controllers
{
    public class PoliciesController : Controller
    {
        [AuthorizeAdmin]
        public IActionResult Index()
        {
            return View();
        }
    }
}
