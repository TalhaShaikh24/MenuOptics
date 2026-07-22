using Microsoft.AspNetCore.Mvc;

namespace MenuOptics.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Success()
        {
            return View();
        }

        public IActionResult Cancel()
        {
            return View();
        }
    }
}
