using MenuOptics.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MenuOptics.Controllers
{
    public class InventoryController : Controller
    {
        [AuthorizeRestaurant]
        public IActionResult Index()
        {
            return View();
        }
        [AuthorizeRestaurant]
        public IActionResult Invoices()
        {
            return View();
        }
    }
}
