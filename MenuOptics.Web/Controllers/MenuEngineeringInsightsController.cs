using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;

namespace MenuOptics.Controllers
{
    public class MenuEngineeringInsightsController : Controller
    {
        [AuthorizeRestaurant]
        public IActionResult Index()
        {
            return View();
        }
    }
}
