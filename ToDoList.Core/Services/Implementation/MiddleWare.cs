using ToDoList_Core.Domain.Enums;
using ToDoList_Core.Domain.Implementation;

namespace ToDoList_Core.Services.Implementation
{
    public class MiddleWare
    {


        User CurrentUser {  get; set; }
        bool isLogged {  get; set; }
        public MiddleWare() { 
            
        }

        // Verificamos el rol del usuario, damos permisos de acuerdo a que rol tenga.
        public UserRole VerifyUserRol(User currentUser) {
            if (isLogged == false) throw new Exception("El usuario no esta logeado");
            return currentUser.Role;

        }
        public void NormalUserCanDo() {
            // Permisos de un usuario normal
        }
        public void AdminUserCanDo()
        {
            // Permisos de Administrador
        }


    }
}
