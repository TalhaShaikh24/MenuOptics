using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;

namespace MenuOptics.Controllers
{
    public class MenuController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MenuController(IConfiguration configuration)
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
    }
}