using System.Security.Claims;
using ToDoList_Core.Domain.Implementation;

namespace ToDoList_API.Extensions
{
    public static class UserClaimsPrincialExtension
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if(int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new InvalidOperationException("El token no contiene un Id de Usuario");

        }
    }
}
