using Microsoft.AspNetCore.Authorization;

namespace ToDoList_API.Authorization.Rule
{
    public class IsDutyOwnerOrAdminRequirement : IAuthorizationRequirement
    {
    }
}
