using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ToDoList_API.Authorization.Rule;
using ToDoList_Core.Domain.Enums;

namespace ToDoList_API.Authorization.RuleHandler
{
    public class IsOwnerOrAdminHandler : AuthorizationHandler<IsOwnerOrAdminRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            IsOwnerOrAdminRequirement requirement)
        {
            if(context.User.HasClaim(ClaimTypes.Role, nameof(UserRole.Admin)))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var userIdFromToken = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(context.Resource is HttpContext httpContext)
            {
                var idFromRoute = httpContext.GetRouteValue("id")?.ToString();
                if(userIdFromToken == idFromRoute)
                {
                    context.Succeed(requirement);
                }
            }
            return Task.CompletedTask;
        }
    }
}
