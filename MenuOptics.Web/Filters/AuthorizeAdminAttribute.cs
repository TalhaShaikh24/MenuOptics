using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Linq;

namespace MenuOptics.Filters
{
    public class AuthorizeAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            if (!httpContext.User.Identity.IsAuthenticated)
            {
                context.HttpContext.Response.Redirect("/User/Login");
                context.Result = new RedirectResult("/User/Login");
                context.HttpContext.Items["Error"] = "Please login to access this page.";
                return;
            }

            var roles = httpContext.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
            
            // ✅ Check for Admin role
            if (!roles.Contains("Admin"))
            {
                context.HttpContext.Response.Redirect("/User/AccessDenied");
                context.Result = new RedirectResult("/User/AccessDenied");
                context.HttpContext.Items["Error"] = "Unauthorized access. Admin role required.";
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
