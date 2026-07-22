using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MenuOptics.Filters;
using MenuOptics.Models;

namespace MenuOptics.Controllers
{
    public class UserController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public UserController(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:5001/");
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeAdmin]
        public IActionResult Index()
        {
            if (HttpContext.Items["Error"] != null)
            {
                TempData["Error"] = HttpContext.Items["Error"].ToString();
            }
            return View();
        }

        [HttpGet]
        [AuthorizeAdmin]
        public IActionResult Users()
        {
            if (HttpContext.Items["Error"] != null)
            {
                TempData["Error"] = HttpContext.Items["Error"].ToString();
            }
            return View();
        }

        
    }
}