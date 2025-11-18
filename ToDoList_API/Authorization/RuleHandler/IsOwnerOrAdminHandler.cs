using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ToDoList_API.Authorization.Rule;
using ToDoList_API.Extensions;
using ToDoList_Core.Domain.Enums;

namespace ToDoList_API.Authorization.RuleHandler
{
    public class IsOwnerOrAdminHandler : AuthorizationHandler<IsOwnerOrAdminRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public IsOwnerOrAdminHandler(IHttpContextAccessor httpContextAccessor) {
            _httpContextAccessor = httpContextAccessor;
        }
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
                var userIdFromToken = context.User.GetUserId().ToString();
                var idFromRoute = _httpContextAccessor.HttpContext?.GetRouteValue("id")?.ToString();

                if (userIdFromToken == idFromRoute)
                {
                    context.Succeed(requirement);
                }

            return Task.CompletedTask;
        }
    }
}
