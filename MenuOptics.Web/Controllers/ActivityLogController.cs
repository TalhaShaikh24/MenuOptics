using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;

namespace MenuOptics.Controllers
{
    [AuthorizeAdmin]
    public class ActivityLogController : Controller
    {
        [HttpGet]
        [ActionName("VendorActivityLog")]
        public IActionResult VendorActivityLog()
        {
            return View("VendorActivityLog");
        }

        [HttpGet]
        [ActionName("UserActivityLog")]
        public IActionResult UserActivityLog()
        {
            return View("UserActivityLog");
        }
    }
}
