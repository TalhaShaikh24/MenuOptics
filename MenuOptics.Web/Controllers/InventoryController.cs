using Microsoft.AspNetCore.Mvc;

namespace MenuOptics.Controllers
{
    public class InventoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Invoices()
        {
            return View();
        }
    }
}
