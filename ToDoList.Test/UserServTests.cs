using Moq;
using System.Linq.Expressions;
using System.Net.Sockets;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Implementation;
using ToDoList_Core.Services.Interfaces;
using Xunit;

namespace ToDoList.Tests
{
    public class UserServiceTests
    {
        // Mocks (Dobles)
        private readonly Mock<IRepository<User>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        // Sujeto de prueba
        private readonly UserService _userService;

        public UserServiceTests()
        {
            // ARRANGE COMÚN
            _mockRepository = new Mock<IRepository<User>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            // Inyectamos los mocks en el servicio real
            _userService = new UserService(_mockRepository.Object, _mockUnitOfWork.Object);
        }

        // ==========================================
        //  TESTS DE "CAMINO FELIZ" (CRUD Básico)
        // ==========================================

        [Fact]
        public async Task CreateUserAsync_ShouldHashPassword_AndCallAdd_AndSaveChanges()
        {
            // Arrange
            string rawPassword = "Password123!";
            var newUser = new User
            {
                Username = "testuser",
                Email = "test@test.com",
                // Simulamos que el DTO ya puso la contraseña plana aquí temporalmente
                // (o que se pasa para ser hasheada)
                PasswordHash = rawPassword
            };

            // Act
            var result = await _userService.CreateUserAsync(newUser);

            // Assert
            // 1. Verificamos que la contraseña YA NO sea la plana
            Assert.NotEqual(rawPassword, result.PasswordHash);
            // 2. Verificamos que parece un hash de BCrypt (empieza con $2)
            Assert.StartsWith("$2", result.PasswordHash);

            // 3. Verificamos interacciones con la BD
            _mockRepository.Verify(r => r.Add(newUser), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task FindUser_ShouldReturnUser_WhenItExists()
        {
            // Arrange
            var expectedUser = new User { Id = 1, Username = "FoundMe" };

            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _userService.FindUser(u => u.Id == 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FoundMe", result.Username);
        }

        [Fact]
        public async Task UpdateUser_ShouldCallUpdateAndSaveChanges()
        {
            // Arrange
            var userToUpdate = new User { Id = 1, Username = "UpdatedName" };

            // Act
            await _userService.UpdateUser(userToUpdate);

            // Assert
            _mockRepository.Verify(r => r.Update(userToUpdate), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_ShouldCallDeleteAndSaveChanges()
        {
            // Arrange
            var userToDelete = new User { Id = 99 };

            // Act
            await _userService.DeleteUser(userToDelete);

            // Assert
            _mockRepository.Verify(r => r.Delete(userToDelete), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==========================================
        //  TESTS DE LÓGICA DE NEGOCIO (Hashing)
        // ==========================================

        [Fact]
        public void HashPassword_ShouldThrowArgumentNull_WhenPasswordIsNull()
        {
            // Arrange
            var userWithNullPass = new User { PasswordHash = null };

            // Act & Assert
            // Esperamos que lance ArgumentNullException si intentamos hashear un nulo
            Assert.Throws<ArgumentNullException>(() =>
                _userService.HashPassword(userWithNullPass));
        }

        [Fact]
        public void HashPassword_ShouldThrowArgumentNull_WhenPasswordIsEmpty()
        {
            // Arrange
            var userWithEmptyPass = new User { PasswordHash = "" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _userService.HashPassword(userWithEmptyPass));
        }

        [Fact]
        public void HashPassword_ShouldReturnValidHash_WhenPasswordIsCorrect()
        {
            // Arrange
            var user = new User { PasswordHash = "MySecret123" };

            // Act
            string hash = _userService.HashPassword(user);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            // Verificamos que sea un hash válido verificándolo con BCrypt real
            Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("MySecret123", hash));
        }

        // ==========================================
        //  TESTS DE ROBUSTEZ (Fallos de BD)
        // ==========================================

        [Fact]
        public async Task FindUser_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.FindUser(u => u.Id == 9999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldPropagateException_WhenSaveFails()
        {
            // Arrange
            var newUser = new User { PasswordHash = "ValidPass" };

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Exception("Error de conexión a BD"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _userService.CreateUserAsync(newUser));

            Assert.Equal("Error de conexión a BD", ex.Message);
        }

        [Fact]
        public async Task UpdateUser_ShouldPropagateException_WhenUpdateFails()
        {
            // Arrange
            var user = new User();
            _mockRepository.Setup(r => r.Update(user))
                .Throws(new InvalidOperationException("Usuario ya modificado"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.UpdateUser(user));

            // Verificamos que NO intentó guardar si el Update falló antes
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // --- TEST DE FILTRADO (LAMBDA) ---
        [Fact]
        public async Task FindUser_ShouldPassPredicateToRepository()
        {
            // Arrange
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(new User());

            // Act
            await _userService.FindUser(u => u.Email == "test@example.com");

            // Assert
            // Verificamos que FindAsync fue llamado con ALGUNA expresión
            _mockRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
        }
        // ==========================================
        //  GRUPO 3: TESTS DE SECUENCIA (ORDEN DE LLAMADAS)
        // ==========================================

        // --- TEST 12: ORDEN EN CREAR ---
        [Fact]
        public async Task CreateUserAsync_ShouldCallAdd_BEFORE_SaveChanges()
        {
            // Arrange
            var user = new User { PasswordHash = "pass" };
            var sequence = new MockSequence(); // Herramienta de Moq para verificar orden

            // Configuramos la expectativa del orden
            _mockRepository.InSequence(sequence).Setup(r => r.Add(user));
            _mockUnitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync());

            // Act
            await _userService.CreateUserAsync(user);

            // Assert
            _mockRepository.Verify(r => r.Add(user), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // --- TEST 13: ORDEN EN ACTUALIZAR ---
        [Fact]
        public async Task UpdateUser_ShouldCallUpdate_BEFORE_SaveChanges()
        {
            var user = new User();
            var sequence = new MockSequence();

            _mockRepository.InSequence(sequence).Setup(r => r.Update(user));
            _mockUnitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync());

            await _userService.UpdateUser(user);

            _mockRepository.Verify(r => r.Update(user), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // --- TEST 14: ORDEN EN BORRAR ---
        [Fact]
        public async Task DeleteUser_ShouldCallDelete_BEFORE_SaveChanges()
        {
            var user = new User();
            var sequence = new MockSequence();

            _mockRepository.InSequence(sequence).Setup(r => r.Delete(user));
            _mockUnitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync());

            await _userService.DeleteUser(user);

            _mockRepository.Verify(r => r.Delete(user), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==========================================
        //  GRUPO 4: INTEGRIDAD Y ROBUSTEZ
        // ==========================================

        // --- TEST 15: CREATE SI FALLA ADD ---
        [Fact]
        public async Task CreateUserAsync_ShouldNotCallSaveChanges_IfRepositoryAddFails()
        {
            // Arrange
            var user = new User { PasswordHash = "pass" };
            // Simulamos que el repositorio explota (ej: memoria llena)
            _mockRepository.Setup(r => r.Add(It.IsAny<User>()))
                .Throws(new OutOfMemoryException());

            // Act & Assert
            await Assert.ThrowsAsync<OutOfMemoryException>(() =>
                _userService.CreateUserAsync(user));

            // Verificamos la integridad: NUNCA se intentó guardar
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // --- TEST 16: DELETE SI FALLA REPOSITORY ---
        [Fact]
        public async Task DeleteUser_ShouldNotCallSaveChanges_IfRepositoryDeleteFails()
        {
            var user = new User();
            _mockRepository.Setup(r => r.Delete(It.IsAny<User>()))
                .Throws(new InvalidOperationException("Error al borrar"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.DeleteUser(user));

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // --- TEST 17: EXCEPCIÓN DE CONCURRENCIA EN UPDATE ---
        [Fact]
        public async Task UpdateUser_ShouldPropagate_ConcurrencyException()
        {
            // Simulamos que otro usuario modificó el registro al mismo tiempo
            var user = new User();
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Conflicto de concurrencia", new List<Microsoft.EntityFrameworkCore.Update.IUpdateEntry>()));

            // El servicio no debe tragar la excepción, debe dejarla subir
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() =>
                _userService.UpdateUser(user));
        }

        // --- TEST 18: RESTRICCIÓN FK EN DELETE ---
        [Fact]
        public async Task DeleteUser_ShouldPropagate_DbUpdateException()
        {
            // Simulamos error porque el usuario tiene Tareas (Duties) asociadas
            var user = new User();
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Error de restricción FK"));

            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
                _userService.DeleteUser(user));
        }

        // ==========================================
        //  GRUPO 5: TESTS DE LÓGICA DE FILTRADO REAL
        // ==========================================

        // --- TEST 19: BUSCAR POR EMAIL (LÓGICA EXACTA) ---
        [Fact]
        public async Task FindUser_ShouldFilterCorrectly_ByEmail()
        {
            // Arrange
            string targetEmail = "juan@test.com";
            var dbData = new List<User>
            {
                new User { Id = 1, Email = "pedro@test.com" },
                new User { Id = 2, Email = targetEmail }, // Este es el que buscamos
                new User { Id = 3, Email = "maria@test.com" }
            };

            // Simulamos que el Mock EJECUTA la lambda real
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((Expression<Func<User, bool>> predicate) =>
                {
                    return dbData.FirstOrDefault(predicate.Compile());
                });

            // Act
            var result = await _userService.FindUser(u => u.Email == targetEmail);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id); // Debe encontrar al ID 2
            Assert.Equal(targetEmail, result.Email);
        }

        // --- TEST 20: BUSCAR POR USERNAME (LÓGICA EXACTA) ---
        [Fact]
        public async Task FindUser_ShouldFilterCorrectly_ByUsername()
        {
            // Arrange
            string targetUser = "admin_pro";
            var dbData = new List<User>
            {
                new User { Id = 1, Username = "usuario1" },
                new User { Id = 2, Username = "usuario2" },
                new User { Id = 3, Username = targetUser }
            };

            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((Expression<Func<User, bool>> predicate) =>
                {
                    return dbData.FirstOrDefault(predicate.Compile());
                });

            // Act
            var result = await _userService.FindUser(u => u.Username == targetUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Id);
        }

        // --- TEST 21: BUSCAR QUE NO EXISTE (LÓGICA EXACTA) ---
        [Fact]
        public async Task FindUser_ShouldReturnNull_WhenFilterDoesNotMatch()
        {
            var dbData = new List<User> { new User { Username = "A" }, new User { Username = "B" } };

            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((Expression<Func<User, bool>> predicate) =>
                {
                    return dbData.FirstOrDefault(predicate.Compile());
                });

            // Buscamos algo que no está
            var result = await _userService.FindUser(u => u.Username == "Z");

            Assert.Null(result);
        }
        // ==========================================
        //  GRUPO 6: SEGURIDAD AVANZADA (HASHING)
        // ==========================================

        // --- TEST 22: VERIFICAR SALTING (HASHES ÚNICOS) ---
        [Fact]
        public void HashPassword_ShouldGenerateDifferentHashes_ForSamePassword()
        {
            // Arrange
            var user1 = new User { PasswordHash = "Password123!" };
            var user2 = new User { PasswordHash = "Password123!" };

            // Act
            // Hasheamos la MISMA contraseña dos veces
            string hash1 = _userService.HashPassword(user1);
            string hash2 = _userService.HashPassword(user2);

            // Assert
            // Deben ser diferentes. Si son iguales, el "Salt" no funciona
            // y tu sistema es vulnerable a ataques de Rainbow Tables.
            Assert.NotEqual(hash1, hash2);

            // Ambos deben ser válidos para la misma contraseña original
            Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("Password123!", hash1));
            Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("Password123!", hash2));
        }

        // --- TEST 23: CREATE FALLA SI PASSWORD ES NULO (INTEGRACIÓN) ---
        [Fact]
        public async Task CreateUserAsync_ShouldThrowArgumentNull_IfPasswordIsNull()
        {
            // Arrange
            var user = new User { PasswordHash = null }; // Inválido

            // Act & Assert
            // El método HashPassword lanzará la excepción, y CreateUser debe dejarla pasar
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _userService.CreateUserAsync(user));

            // Verificamos que NUNCA se tocó la base de datos
            _mockRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // ==========================================
        //  GRUPO 7: RESTRICCIONES DE BASE DE DATOS (UNICIDAD)
        // ==========================================

        // --- TEST 24: EMAIL DUPLICADO EN CREATE ---
        [Fact]
        public async Task CreateUserAsync_ShouldPropagate_DbUpdateException_OnDuplicateEmail()
        {
            // Arrange
            var user = new User { PasswordHash = "pass" };
            // Simulamos el error que lanza SQL Server cuando violas un UNIQUE INDEX
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Email duplicado"));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
                _userService.CreateUserAsync(user));

            Assert.Equal("Email duplicado", ex.Message);
        }

        // --- TEST 25: USERNAME DUPLICADO EN UPDATE ---
        [Fact]
        public async Task UpdateUser_ShouldPropagate_DbUpdateException_OnDuplicateUsername()
        {
            var user = new User();
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Username duplicado"));

            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
                _userService.UpdateUser(user));
        }

        // ==========================================
        //  GRUPO 8: FILTRADO COMPLEJO Y LÓGICA
        // ==========================================

        // --- TEST 26: BUSCAR POR NOMBRE Y APELLIDO (CONDICIÓN MÚLTIPLE) ---
        [Fact]
        public async Task FindUser_ShouldFilterCorrectly_WithMultipleConditions()
        {
            // Arrange
            var dbData = new List<User>
            {
                new User { Id = 1, Name = "Juan", LastName = "Perez" },
                new User { Id = 2, Name = "Juan", LastName = "Gomez" }, // Coincide nombre, no apellido
                new User { Id = 3, Name = "Maria", LastName = "Perez" }
            };

            // Mock inteligente con lambda real
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((Expression<Func<User, bool>> predicate) =>
                {
                    return dbData.FirstOrDefault(predicate.Compile());
                });

            // Act
            // Buscamos a "Juan Perez"
            var result = await _userService.FindUser(u => u.Name == "Juan" && u.LastName == "Perez");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id); // Debe ser el primero
        }

        // --- TEST 27: UPDATE CON OBJETO NULO ---
        [Fact]
        public async Task UpdateUser_ShouldThrow_WhenRepositoryThrows_OnNull()
        {
            // Arrange
            // Simulamos que el repositorio no acepta nulos (defensa en profundidad)
            _mockRepository.Setup(r => r.Update(null!))
                .Throws(new ArgumentNullException());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _userService.UpdateUser(null!));
        }

        // ==========================================
        //  GRUPO 9: ROBUSTEZ DEL SISTEMA
        // ==========================================

        // --- TEST 28: FALLO EN REPOSITORIO FIND (EJ. CONEXIÓN CAÍDA) ---
        [Fact]
        public async Task FindUser_ShouldThrow_WhenConnectionFails()
        {
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ThrowsAsync(new SocketException()); // Simulamos error de red

            await Assert.ThrowsAsync<SocketException>(() =>
                _userService.FindUser(u => u.Id == 1));
        }

        // --- TEST 29: VERIFICAR QUE UPDATE NO LLAMA A ADD ---
        [Fact]
        public async Task UpdateUser_ShouldNeverCallAdd()
        {
            var user = new User();
            await _userService.UpdateUser(user);

            // Aseguramos que no estamos duplicando registros accidentalmente
            _mockRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        }

        // --- TEST 30: VERIFICAR QUE DELETE NO LLAMA A UPDATE ---
        [Fact]
        public async Task DeleteUser_ShouldNeverCallUpdate()
        {
            var user = new User();
            await _userService.DeleteUser(user);

            // Aseguramos que borramos limpiamente sin intentar modificar antes
            _mockRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        // --- TEST 31: CREATE CON NOMBRE DE USUARIO VACÍO (VALIDACIÓN) ---
        // Aunque el modelo tiene [Required], el servicio debería intentar guardar.
        // Probamos que si el repo falla por validación, el servicio lo reporta.
        [Fact]
        public async Task CreateUserAsync_ShouldFail_IfEntityIsInvalid()
        {
            var invalidUser = new User { PasswordHash = "pass" }; // Falta Username/Email

            // EF Core lanzaría esto si intentas guardar una entidad inválida
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Campos requeridos faltantes"));

            var ex = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
                _userService.CreateUserAsync(invalidUser));

            Assert.Contains("Campos requeridos", ex.Message);
        }
    }
}