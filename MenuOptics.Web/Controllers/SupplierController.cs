using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;

namespace MenuOptics.Controllers
{
    public class SupplierController : Controller
    {
        [AuthorizeRestaurant]
        public IActionResult Index()
        {
            return View();
        }
    }
}
