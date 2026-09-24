using MenuOptics.Models;
using MenuOptics.Models.Enums;
using MenuOptics.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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

            var userId = int.TryParse(context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;
            var userResult = await _userService.GetUserAsync(userId);
            var user = userResult.Data as UserDto;

            if (user == null)
            {
                context.Result = new UnauthorizedObjectResult(new ResponseDto
                {
                    Status = 401,
                    ResponseMsg = "User not found."
                });
                return;
            }

            // Admin and Vendor accounts do not require restaurant subscriptions
            if (!string.IsNullOrEmpty(user.Roles) &&
                (user.Roles.Contains(Role.Admin.ToString(), StringComparison.OrdinalIgnoreCase) ||
                 user.Roles.Contains(Role.Vendor.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                await next();
                return;
            }

            // If user is a restaurant staff member (has ParentId), evaluate the parent restaurant's subscription
            var targetUser = user;
            if (user.ParentId.HasValue && user.ParentId.Value > 0)
            {
                var parentResult = await _userService.GetUserAsync(user.ParentId.Value);
                if (parentResult.Data is UserDto parentUser)
                {
                    targetUser = parentUser;
                }
            }

            // If restaurant was created by a Vendor, it is completely free to use (no subscription required)
            bool isSubscriptionActive = targetUser.SubscriptionStatus == "Active";

            if (isSubscriptionActive)
            {
                await next();
                return;
            }

            var status = targetUser.SubscriptionStatus;
            bool isActive = !string.IsNullOrEmpty(status) &&
                (status.Equals(PaymentStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase) ||
                 status.Equals(PaymentStatus.Trialing.ToString(), StringComparison.OrdinalIgnoreCase));

            if (!isActive)
            {
                context.Result = new ObjectResult(new ResponseDto
                {
                    Status = 402,
                    ResponseMsg = $"Your subscription status is {status ?? "Pending"}. An active subscription is required.",
                    Data = new
                    {
                        SubscriptionStatus = status ?? PaymentStatus.Pending.ToString(),
                        IsParent = !user.ParentId.HasValue
                    }
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
