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
            
            // ✅ Check for either Admin or Restaurant role
            if (!roles.Contains("Admin") && !roles.Contains("Restaurant"))
            {
                if (roles.Contains("Vendor"))
                {
                    context.HttpContext.Response.Redirect("/Ingredient/VendorIngredients");
                    context.Result = new RedirectResult("/Ingredient/VendorIngredients");
                    return;
                }

                context.HttpContext.Response.Redirect("/User/AccessDenied");
                context.Result = new RedirectResult("/User/AccessDenied");
                context.HttpContext.Items["Error"] = "Unauthorized access. Admin or Restaurant role required.";
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
