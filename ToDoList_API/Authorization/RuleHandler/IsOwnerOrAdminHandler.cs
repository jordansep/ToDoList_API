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
            var userRole = context.User.FindFirstValue(ClaimTypes.Role);

            // Si es Admin, tiene acceso total
            if (userRole == UserRole.Admin.ToString() ||
                userRole == ((int)UserRole.Admin).ToString())
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Si no es Admin, verificar que sea el dueño del recurso
            if (context.Resource is HttpContext httpContext)
            {
                var userIdFromToken = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var idFromRoute = httpContext.GetRouteValue("id")?.ToString();

                if (userIdFromToken == idFromRoute)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
