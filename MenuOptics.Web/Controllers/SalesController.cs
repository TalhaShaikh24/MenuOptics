using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;

namespace MenuOptics.Controllers
{
    public class SalesController : Controller
    {
        
        [AuthorizeAdmin]
        public IActionResult Index()
        {
            return View();
        }
    }
}
