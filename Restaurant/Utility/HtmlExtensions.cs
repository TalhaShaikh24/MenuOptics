using Microsoft.AspNetCore.Mvc.Rendering;

namespace MenuOptics.Utility
{
    public static class HtmlExtensions
    {
        public static string IsActive(this IHtmlHelper html, string controller, string action = null)
        {
            var routeData = html.ViewContext.RouteData.Values;
            var routeAction = routeData["action"]?.ToString();
            var routeController = routeData["controller"]?.ToString();

            if (string.Equals(controller, routeController, StringComparison.OrdinalIgnoreCase) &&
                (action == null || string.Equals(action, routeAction, StringComparison.OrdinalIgnoreCase)))
            {
                return "active";
            }

            return "";
        }
    }

}
