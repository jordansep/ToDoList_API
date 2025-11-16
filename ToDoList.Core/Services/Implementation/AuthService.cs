using Microsoft.Extensions.Configuration; // ¡AÑADE ESTE USING!
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;     // ¡AÑADE ESTE USING! (Para el token)
using System.Data;
using System.IdentityModel.Tokens.Jwt; // ¡AÑADE ESTE USING! (Para el token)
using System.Security.Claims;          // ¡AÑADE ESTE USING! (Para el token)
using System.Text;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;

namespace ToDoList_Core.Services.Implementation
{
    public class AuthService : IAuthService
    {
        protected readonly IUserService _userService;
        protected readonly IConfiguration _config;
        public AuthService(IUserService userService, IConfiguration config)
        {
            _userService = userService;
            _config = config;
        }
        public async Task<string> LoginAsync(string username, string password)
        {
            // Buscamos un usuario basado en el Email
            var user = await _userService.FindUser(u => u.Email == username || u.Username == username);
            if (user == null) return null;
            // Verificamos que la contraseña y el hash coincidan
            if (!BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
            {
                return "Contraseña incorrecta";
            } 
            string authToken = CreateToken(user);
            return authToken;
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            // ⚠️ CodeArchitect Fix: Protección contra Nulos
            // Intentamos leer del JSON. Si falla (??), usamos la clave de respaldo.
            var tokenKey = _config.GetSection("AppSettings:Token").Value
 ?? throw new InvalidOperationException("La clave JWT no está configurada en appsettings.json");

            if (tokenKey.Length < 64)
            {
                throw new ArgumentException("La clave JWT debe tener al menos 64 caracteres.");
            }

          var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

   var tokenDescriptor = new SecurityTokenDescriptor
            {
   Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
        SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
        }

    }
}
