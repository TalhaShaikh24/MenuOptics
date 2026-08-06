using MenuOptics.Data;
using MenuOptics.Models;
using MenuOptics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Data;
using System.Security.Claims;

namespace MenuOptics.API.Middleware
{
    public class SubscriptionRequiredFilter : IAsyncActionFilter
    {
        private readonly IUserService _userService;

        public SubscriptionRequiredFilter(IUserService userService)
        {
            _userService = userService;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {

            // Skip subscription check if [IgnoreSubscription] is applied
            var ignore = context.ActionDescriptor.EndpointMetadata
                .OfType<IgnoreSubscriptionAttribute>()
                .Any();

            if (ignore)
            {
                await next();
                return;
            }



            var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<DapperContext>();

            using IDbConnection db = dbContext.CreateConnection();

            var userId = int.TryParse(context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;
            var user = (UserDto?)(await _userService.GetUserAsync(userId)).Data;

            if (user == null)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Message = "User not found."
                });
                return;
            }

            if (user.SubscriptionStatus == null || !user.SubscriptionStatus.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new ResponseDto
                {
                    Status = 402,
                    ResponseMsg = $"Your subscription status is {user.SubscriptionStatus}.",
                })
                {
                    StatusCode = StatusCodes.Status402PaymentRequired
                };
                return;
            }

            await next();
        }
    }
}
