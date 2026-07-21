using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;

namespace MenuOptics.Controllers
{
    public class IngredientController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public IngredientController(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
        }

        
        [AuthorizeAdmin]
        public IActionResult Index()
        {
            if (HttpContext.Items["Error"] != null)
            {
                TempData["Error"] = HttpContext.Items["Error"].ToString();
            }
            return View();
        }

        [Authorize]
        public IActionResult VendorIngredients()
        {
            if (HttpContext.Items["Error"] != null)
            {
                TempData["Error"] = HttpContext.Items["Error"].ToString();
            }
            return View();
        }
    }
}