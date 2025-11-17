using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using System.Security.Claims;
using ToDoList_API.Authorization.Rule;
using ToDoList_API.Extensions;
using ToDoList_Core.Domain.Enums;
using ToDoList_Core.Services.Interfaces;


namespace ToDoList_API.Authorization.RuleHandler
{
    public class IsDutyOwnerOrAdminHandler : AuthorizationHandler<IsDutyOwnerOrAdminRequirement>
    {
        private readonly IDutyService _dutyService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public IsDutyOwnerOrAdminHandler(IDutyService dutyService, IHttpContextAccessor httpContextAccessor)
        {
            _dutyService = dutyService;
            _httpContextAccessor = httpContextAccessor;
        }
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            IsDutyOwnerOrAdminRequirement requirement) // <-- Arreglado
        {
            if (context.User.HasClaim(ClaimTypes.Role, nameof(UserRole.Admin)))
            {
                context.Succeed(requirement);
                return;
            }
            var userIdFromClaim = context.User.GetUserId();
            var dutyIdString = _httpContextAccessor.HttpContext?.GetRouteValue("id")?.ToString();
            Debug.WriteLine("--- INICIO DE POLÍTICA 'IsDutyOwnerOrAdmin' ---");
            Debug.WriteLine($"[Auth] ID del Usuario (del Token): {userIdFromClaim}");
            Debug.WriteLine($"[Auth] ID de la Tarea (de la Ruta): {dutyIdString}");
            if (!int.TryParse(dutyIdString, out int dutyIdFromRoute))
            {
                Debug.WriteLine($"[Auth] Fallo: Tarea con ID {dutyIdFromRoute} no encontrada en la BD.");
                context.Fail();
                return;
            }

            var duty = await _dutyService.FindDuty(d => d.Id == dutyIdFromRoute);
            if(duty == null) {
                Debug.WriteLine("[Auth] Éxito: El dueño coincide.");
                context.Fail();
                return;
            }
            Debug.WriteLine($"[Auth] Comprobando propiedad: ¿Es {duty.UserID} (Dueño BD) == {userIdFromClaim} (Dueño Token)?");
            Debug.WriteLine($"[Auth] ID del Token: {userIdFromClaim}");
            Debug.WriteLine($"[Auth] ID del Dueño de la Tarea: {duty.UserID}");
            if (duty.UserID == userIdFromClaim)
            {

                context.Succeed(requirement);
                return;
            }
            Debug.WriteLine("[Auth] Fallo: El dueño no coincide.");
            context.Fail();
        }
    }
}
