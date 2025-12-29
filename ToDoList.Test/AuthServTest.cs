using Moq;
using Xunit;
using Microsoft.Extensions.Configuration;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Implementation;
using ToDoList_Core.Services.Interfaces;
using ToDoList_Core.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;

namespace ToDoList.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IConfigurationSection> _mockConfigSection;
        private readonly AuthService _authService;

        private const string ValidSecretKey = "EstaEsUnaClaveSuperSecretaYLoSuficientementeLargaParaQueHMACSha512NoSeQueje1234567890";

        public AuthServiceTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfigSection = new Mock<IConfigurationSection>();

            _mockConfigSection.Setup(s => s.Value).Returns(ValidSecretKey);
            _mockConfig.Setup(c => c.GetSection("AppSettings:Token")).Returns(_mockConfigSection.Object);

            _authService = new AuthService(_mockUserService.Object, _mockConfig.Object);
        }

        // ... (Tests 1, 2, 3, 4 y 5 se quedan igual, funcionaban bien) ...
        // Te los incluyo resumidos para que el archivo esté completo

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            string password = "Password123!";
            string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);
            var user = new User { Id = 1, Username = "test", PasswordHash = passwordHash, Role = UserRole.NormalUser, Email = "test@test.com" };

            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            var token = await _authService.LoginAsync("test", password);
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenUserDoesNotExist()
        {
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);
            var result = await _authService.LoginAsync("ghost", "pass");
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnErrorMessage_WhenPasswordIsInvalid()
        {
            string correctHash = BCrypt.Net.BCrypt.EnhancedHashPassword("CorrectPass");
            var user = new User { Username = "test", PasswordHash = correctHash, Email = "test@test.com" }; // Email añadido por seguridad

            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);
            var result = await _authService.LoginAsync("test", "WrongPass");
            Assert.Equal("Contraseña incorrecta", result);
        }

        [Fact]
        public async Task LoginAsync_ShouldWork_WhenLoggingInWithEmail()
        {
            string password = "pass";
            string email = "user@email.com";
            var user = new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password) };

            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);
            var token = await _authService.LoginAsync(email, password);
            Assert.NotNull(token);
        }

        // --- TEST 5 CORREGIDO ---
        [Fact]
        public async Task LoginAsync_ShouldThrowInvalidOperation_WhenConfigKeyIsMissing()
        {
            // Arrange
            var user = new User
            {
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"),
                Email = "test@test.com"
            };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Simulamos que appsettings devuelve NULL
            _mockConfigSection.Setup(s => s.Value).Returns((string?)null);

            // Act & Assert
            // ✅ CORRECCIÓN: Ahora esperamos InvalidOperationException
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.LoginAsync("user", "pass"));

            // Opcional: Verificar que el mensaje sea el esperado
            Assert.Equal("La clave JWT no está configurada en appsettings.json", ex.Message);
        }

        // --- CORRECCIÓN TEST 6: AÑADIDO EMAIL ---
        [Fact]
        public async Task LoginAsync_ShouldThrowArgumentException_WhenKeyIsTooShort()
        {
            // Arrange
            // ¡CORRECCIÓN! Añadimos Email para evitar ArgumentNullException en CreateToken
            var user = new User
            {
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"),
                Email = "test@test.com"
            };

            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Clave corta
            _mockConfigSection.Setup(s => s.Value).Returns("ClaveCorta123");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _authService.LoginAsync("user", "pass"));

            Assert.Contains("64 caracteres", ex.Message);
        }

        // --- CORRECCIÓN TEST 7: NOMBRES DE CLAIMS ---
        [Fact]
        public async Task LoginAsync_GeneratedToken_ShouldContainCorrectClaims()
        {
            // Arrange
            var user = new User
            {
                Id = 99,
                Email = "claim@test.com",
                Role = UserRole.Admin,
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass")
            };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var tokenString = await _authService.LoginAsync("user", "pass");

            // Assert & Inspect
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            // ¡CORRECCIÓN! Buscamos por los nombres estándar de JWT (short names)
            // nameid = NameIdentifier, email = Email, role = Role
            Assert.Equal("99", jwtToken.Claims.First(c => c.Type == "nameid").Value);
            Assert.Equal("claim@test.com", jwtToken.Claims.First(c => c.Type == "email").Value);
            Assert.Equal("Admin", jwtToken.Claims.First(c => c.Type == "role").Value);
        }

        // --- CORRECCIÓN TEST 8: AÑADIDO EMAIL ---
        [Fact]
        public async Task LoginAsync_GeneratedToken_ShouldHaveValidExpiration()
        {
            // Arrange
            // ¡CORRECCIÓN! Añadimos Email
            var user = new User
            {
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"),
                Email = "test@test.com"
            };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var tokenString = await _authService.LoginAsync("user", "pass");
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            // Assert
            Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
            Assert.True((jwtToken.ValidTo - DateTime.UtcNow).TotalHours <= 25);
        }

        [Fact]
        public async Task LoginAsync_ShouldCallFindUser_ExactlyOnce()
        {
            // Arrange (Needs valid user to proceed)
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"), Email = "test@test.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            await _authService.LoginAsync("user", "pass");

            _mockUserService.Verify(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
        }

        // --- CORRECCIÓN TEST 10: AÑADIDO EMAIL ---
        [Fact]
        public async Task LoginAsync_Token_ShouldUseHmacSha512()
        {
            // Arrange
            // ¡CORRECCIÓN! Añadimos Email
            var user = new User
            {
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"),
                Email = "test@test.com"
            };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var tokenString = await _authService.LoginAsync("user", "pass");
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            // Assert
            Assert.Equal("HS512", jwtToken.SignatureAlgorithm);
        }
        // ==========================================
        //  GRUPO 5: VALIDACIÓN DE ENTRADAS Y ROBUSTEZ
        // ==========================================

        // --- TEST 11: NOMBRE DE USUARIO NULO ---
        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenUsernameIsNull()
        {
            // Arrange
            // No necesitamos mockear el usuario porque debería fallar antes o en la búsqueda
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.LoginAsync(null, "pass");

            // Assert
            // Si pasamos null, FindUser no encontrará nada y devolverá null.
            // Esto confirma que no explota con NullReferenceException.
            Assert.Null(result);
        }

        // --- TEST 12: CONTRASEÑA NULA ---
        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsNull()
        {
            // Arrange
            var user = new User { PasswordHash = "hash", Email = "test@test.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Act & Assert
            // BCrypt lanzará una excepción si le pasamos null como contraseña a verificar.
            // Esperamos que el servicio propague esa excepción o falle controladamente.
            // (En tu implementación actual, BCrypt lanzará ArgumentNullException)
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authService.LoginAsync("user", null));
        }

        // --- TEST 13: USUARIO EN BD SIN EMAIL (DATOS CORRUPTOS) ---
        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenUserInDbHasNoEmail()
        {
            // Arrange
            var password = "pass";
            var userWithoutEmail = new User
            {
                Id = 1,
                Username = "user",
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password),
                Email = null! // ¡Dato corrupto!
            };

            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(userWithoutEmail);

            // Act & Assert
            // Al intentar crear el Claim del email con valor null, el constructor de Claim lanzará excepción.
            // Esto nos avisa que tenemos datos sucios en la BD.
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _authService.LoginAsync("user", password));
        }

        // --- TEST 14: FALLO EN REPOSITORIO (FIND USER) ---
        [Fact]
        public async Task LoginAsync_ShouldPropagateException_WhenRepositoryFails()
        {
            // Arrange
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>()))
                .ThrowsAsync(new TimeoutException("DB Timeout"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
                _authService.LoginAsync("user", "pass"));

            Assert.Equal("DB Timeout", ex.Message);
        }

        // ==========================================
        //  GRUPO 6: DETALLES TÉCNICOS DEL TOKEN (CLAIMS EXTRA)
        // ==========================================

        // --- TEST 15: TOKEN TIENE FECHA DE EMISIÓN (IAT) ---
        [Fact]
        public async Task LoginAsync_Token_ShouldHaveIssuedAtClaim()
        {
            // Arrange
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"), Email = "a@b.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var tokenString = await _authService.LoginAsync("user", "pass");
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            // Assert
            // "iat" (Issued At) es un claim estándar que indica cuándo se creó el token
            Assert.Contains(jwtToken.Claims, c => c.Type == "iat"); // Nombre corto estándar
            // Opcional: Verificar que sea reciente (menos de 1 minuto)
            var iat = jwtToken.IssuedAt;
            Assert.True((DateTime.UtcNow - iat).TotalMinutes < 1);
        }

        // --- TEST 16: TOKEN TIENE "NOT BEFORE" (NBF) ---
        [Fact]
        public async Task LoginAsync_Token_ShouldHaveNotBeforeClaim()
        {
            // Arrange
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"), Email = "a@b.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var tokenString = await _authService.LoginAsync("user", "pass");
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            // Assert
            // "nbf" indica desde cuándo es válido el token (usualmente "ahora mismo")
            Assert.Contains(jwtToken.Claims, c => c.Type == "nbf");
            Assert.True(jwtToken.ValidFrom <= DateTime.UtcNow);
        }

        // --- TEST 17: PROBAR ROL DIFERENTE (ADMIN) ---
        [Fact]
        public async Task LoginAsync_ShouldGenerateTokenWithAdminRole_WhenUserIsAdmin()
        {
            // Arrange
            var user = new User
            {
                Role = UserRole.Admin, // Usuario Admin
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"),
                Email = "admin@test.com"
            };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var tokenString = await _authService.LoginAsync("admin", "pass");
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            // Assert
            var roleClaim = jwtToken.Claims.First(c => c.Type == "role").Value;
            Assert.Equal("Admin", roleClaim); // Debe decir "Admin", no "NormalUser"
        }

        // ==========================================
        //  GRUPO 7: CASOS DE CONTRASEÑA Y CONFIGURACIÓN
        // ==========================================

        // --- TEST 18: CONTRASEÑA ES CASE SENSITIVE ---
        [Fact]
        public async Task LoginAsync_ShouldFail_WhenPasswordCaseIsIncorrect()
        {
            // Arrange
            var correctPass = "MyPassword123";
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(correctPass), Email = "a@b.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            // Intentamos loguear con "mypassword123" (minúsculas)
            var result = await _authService.LoginAsync("user", correctPass.ToLower());

            // Assert
            Assert.Equal("Contraseña incorrecta", result);
        }

        // --- TEST 19: CLAVE EN EL LÍMITE EXACTO (64 CARACTERES) ---
        [Fact]
        public async Task LoginAsync_ShouldWork_WhenKeyIsExactly64Chars()
        {
            // Arrange
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"), Email = "a@b.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Creamos una clave de exactamente 64 caracteres
            string key64 = new string('a', 64);
            _mockConfigSection.Setup(s => s.Value).Returns(key64);

            // Act
            var token = await _authService.LoginAsync("user", "pass");

            // Assert
            Assert.NotNull(token); // Debería pasar la validación de longitud
        }

        // --- TEST 20: CONFIGURACIÓN: SECCIÓN NO EXISTENTE ---
        [Fact]
        public async Task LoginAsync_ShouldThrowInvalidOperation_WhenConfigSectionReturnNull()
        {
            // Arrange
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"), Email = "test@test.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            // Simulamos que .GetSection devuelve algo, pero .Value es nulo (clave vacía o no existente)
            _mockConfigSection.Setup(s => s.Value).Returns((string?)null);

            // Act & Assert
            // Debería lanzar la misma excepción que configuramos en AuthService para "clave faltante"
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _authService.LoginAsync("user", "pass"));
        }
        // ==========================================
        //  GRUPO 8: CASOS EXTREMOS DE ENTRADA
        // ==========================================

        // --- TEST 21: USUARIO VACÍO (STRING.EMPTY) ---
        [Fact]
        public async Task LoginAsync_ShouldReturnNull_WhenUsernameIsEmptyString()
        {
            // Arrange
            // El repositorio probablemente devuelva null si busca ""
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authService.LoginAsync("", "pass");

            // Assert
            Assert.Null(result);
        }

        // --- TEST 22: CONTRASEÑA VACÍA (STRING.EMPTY) ---
        [Fact]
        public async Task LoginAsync_ShouldReturnErrorMessage_WhenPasswordIsEmptyString()
        {
            // Arrange
            // Un usuario válido, pero la contraseña guardada es "algo"
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"), Email = "a@b.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            // Intentamos loguear con contraseña vacía
            var result = await _authService.LoginAsync("user", "");

            // Assert
            // BCrypt fallará al comparar "" con el hash real
            Assert.Equal("Contraseña incorrecta", result);
        }

        // --- TEST 23: SOPORTE UNICODE (EMOJIS/CARACTERES ESPECIALES) ---
        [Fact]
        public async Task LoginAsync_ShouldWork_WithUnicodePassword()
        {
            // Arrange
            string unicodePass = "P@sswörd🔑!ñ"; // Contraseña compleja
            var user = new User
            {
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(unicodePass),
                Email = "a@b.com"
            };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var token = await _authService.LoginAsync("user", unicodePass);

            // Assert
            Assert.NotNull(token);
            Assert.NotEqual("Contraseña incorrecta", token);
        }

        // --- TEST 24: CONTRASEÑA MUY LARGA (>72 CARACTERES) ---
        [Fact]
        public async Task LoginAsync_ShouldWork_WithLongPasswords()
        {
            // Arrange
            // BCrypt original tenía un límite de 72 bytes. EnhancedHashPassword debería manejarlo
            // o al menos comportarse consistentemente. Probamos una pass de 100 chars.
            string longPass = new string('x', 100);
            var user = new User
            {
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(longPass),
                Email = "a@b.com"
            };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var token = await _authService.LoginAsync("user", longPass);

            // Assert
            Assert.NotNull(token);
            Assert.NotEqual("Contraseña incorrecta", token);
        }

        // ==========================================
        //  GRUPO 9: LIMPIEZA DEL TOKEN
        // ==========================================

        // --- TEST 25: NO DEBE INCLUIR ISSUER NI AUDIENCE POR DEFECTO ---
        [Fact]
        public async Task LoginAsync_Token_ShouldNotContainIssuerOrAudience_WhenNotConfigured()
        {
            // Arrange
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("pass"), Email = "a@b.com" };
            _mockUserService.Setup(u => u.FindUser(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            // Act
            var tokenString = await _authService.LoginAsync("user", "pass");
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            // Assert
            // Como en tu código actual de 'CreateToken' no asignaste .Issuer ni .Audience
            // verificamos que el token NO tenga esos claims para evitar problemas de validación inesperados.
            Assert.DoesNotContain(jwtToken.Claims, c => c.Type == "iss"); // Issuer
            Assert.DoesNotContain(jwtToken.Claims, c => c.Type == "aud"); // Audience
        }
    }
}