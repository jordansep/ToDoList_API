using Moq;
using System.Linq.Expressions;
using ToDoList.Core.Domain.UseCases;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;
using Xunit;
using ToDoList.Core.Domain.UseCases.Implementation;

namespace ToDoList.Tests
{
    public class UseCasesTests
    {
        private readonly Mock<IRepository<User>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;

        public UseCasesTests()
        {
            _mockRepository = new Mock<IRepository<User>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
        }

        // ==========================================
        //  TESTS PARA: ChangeUserPassword
        // ==========================================

        [Fact]
        public async Task ChangePassword_ShouldThrow_IfOldPasswordIsIncorrect()
        {
            // Arrange
            var useCase = new ChangePasswordAsync(_mockRepository.Object, _mockUnitOfWork.Object);

            string realHash = BCrypt.Net.BCrypt.EnhancedHashPassword("CorrectOldPass");
            var user = new User { Id = 1, PasswordHash = realHash };

            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            var input = new ChangePasswordInput
            {
                OldPassword = "WrongOldPass",
                NewPassword = "NewPass123!"
            };

            // Act & Assert
            // Debe fallar porque la contraseña vieja no coincide
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                useCase.Execute(1, input));

            // Aseguramos que NO se guardó nada
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task ChangePassword_ShouldUpdateHash_IfOldPasswordIsCorrect()
        {
            // Arrange
            var useCase = new ChangePasswordAsync(_mockRepository.Object, _mockUnitOfWork.Object);

            string realHash = BCrypt.Net.BCrypt.EnhancedHashPassword("CorrectOldPass");
            var user = new User { Id = 1, PasswordHash = realHash };

            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            var input = new ChangePasswordInput
            {
                OldPassword = "CorrectOldPass",
                NewPassword = "NewPass123!"
            };

            // Act
            await useCase.Execute(1, input);

            // Assert
            // Verificamos que el hash cambió (no es igual al viejo ni a la nueva pass plana)
            Assert.NotEqual(realHash, user.PasswordHash);
            Assert.NotEqual("NewPass123!", user.PasswordHash);

            // Verificamos que se guardó
            _mockRepository.Verify(r => r.Update(user), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==========================================
        //  TESTS PARA: ChangeUserEmail
        // ==========================================

        [Fact]
        public async Task ChangeEmail_ShouldThrow_IfPasswordIsIncorrect()
        {
            // Arrange
            var useCase = new ChangeUserEmailAsync(_mockRepository.Object, _mockUnitOfWork.Object);

            string realHash = BCrypt.Net.BCrypt.EnhancedHashPassword("MyPass");
            var user = new User { Id = 1, PasswordHash = realHash, Email = "old@test.com" };

            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user); // El primer FindAsync devuelve al usuario

            var input = new ChangeEmailInput
            {
                NewEmail = "new@test.com",
                Password = "WrongPassword"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                useCase.Execute(1, input));
        }

        [Fact]
        public async Task ChangeEmail_ShouldThrow_IfEmailIsTakenByAnotherUser()
        {
            // Arrange
            var useCase = new ChangeUserEmailAsync(_mockRepository.Object, _mockUnitOfWork.Object);

            string realHash = BCrypt.Net.BCrypt.EnhancedHashPassword("MyPass");
            var currentUser = new User { Id = 1, PasswordHash = realHash, Email = "old@test.com" };
            var otherUser = new User { Id = 2, Email = "taken@test.com" };

            // Simulamos un Mock secuencial inteligente
            _mockRepository.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(currentUser) // 1ra llamada: Encuentra al usuario actual
                .ReturnsAsync(otherUser);  // 2da llamada: Encuentra que el email está ocupado

            var input = new ChangeEmailInput
            {
                NewEmail = "taken@test.com",
                Password = "MyPass"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                useCase.Execute(1, input));

            Assert.Contains("uso", ex.Message); // "El email ya está en uso..."
        }

        [Fact]
        public async Task ChangeEmail_ShouldUpdateEmail_IfValid()
        {
            // Arrange
            var useCase = new ChangeUserEmailAsync(_mockRepository.Object, _mockUnitOfWork.Object);

            string realHash = BCrypt.Net.BCrypt.EnhancedHashPassword("MyPass");
            var user = new User { Id = 1, PasswordHash = realHash, Email = "old@test.com" };

            _mockRepository.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user)  // 1. Encuentra usuario
                .ReturnsAsync((User?)null); // 2. No encuentra duplicados (email libre)

            var input = new ChangeEmailInput
            {
                NewEmail = "brandnew@test.com",
                Password = "MyPass"
            };

            // Act
            await useCase.Execute(1, input);

            // Assert
            Assert.Equal("brandnew@test.com", user.Email);
            _mockRepository.Verify(r => r.Update(user), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
        // ==========================================
        //  GRUPO 2: ChangeUserPassword (ROBUSTEZ Y BORDES)
        // ==========================================

        // --- TEST 6: USUARIO NO ENCONTRADO ---
        [Fact]
        public async Task ChangePassword_ShouldThrowKeyNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var useCase = new ChangePasswordAsync(_mockRepository.Object, _mockUnitOfWork.Object);

            // Simulamos que no se encuentra el usuario
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            var input = new ChangePasswordInput { OldPassword = "Any", NewPassword = "New" };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                useCase.Execute(1, input));
        }

        // --- TEST 7: FALLO DE BASE DE DATOS (AL GUARDAR) ---
        [Fact]
        public async Task ChangePassword_ShouldPropagateException_WhenSaveFails()
        {
            // Arrange
            var useCase = new ChangePasswordAsync(_mockRepository.Object, _mockUnitOfWork.Object);

            string realHash = BCrypt.Net.BCrypt.EnhancedHashPassword("OldPass");
            var user = new User { Id = 1, PasswordHash = realHash };

            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Exception("Error SQL Crítico"));

            var input = new ChangePasswordInput { OldPassword = "OldPass", NewPassword = "New" };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                useCase.Execute(1, input));

            Assert.Equal("Error SQL Crítico", ex.Message);
        }

        // --- TEST 8: INTEGRIDAD (NO ACTUALIZAR SI LA CONTRASEÑA ESTÁ MAL) ---
        [Fact]
        public async Task ChangePassword_ShouldNotUpdateUser_IfOldPasswordIsWrong()
        {
            // Arrange
            var useCase = new ChangePasswordAsync(_mockRepository.Object, _mockUnitOfWork.Object);
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("RealPass") };

            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            var input = new ChangePasswordInput { OldPassword = "WrongPass", NewPassword = "New" };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.Execute(1, input));

            // Verificación Crítica: Nunca se debe llamar a Update si la validación falla
            _mockRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        }

        // --- TEST 9: VERIFICAR HASHING REAL ---
        [Fact]
        public async Task ChangePassword_ShouldHashTheNewPassword_NotStoreItPlain()
        {
            // Arrange
            var useCase = new ChangePasswordAsync(_mockRepository.Object, _mockUnitOfWork.Object);
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("Old") };
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            var input = new ChangePasswordInput { OldPassword = "Old", NewPassword = "MyNewSecretPassword" };

            // Act
            await useCase.Execute(1, input);

            // Assert
            // La contraseña guardada NO debe ser igual al texto plano
            Assert.NotEqual("MyNewSecretPassword", user.PasswordHash);
            // Debe ser un hash válido de esa contraseña
            Assert.True(BCrypt.Net.BCrypt.EnhancedVerify("MyNewSecretPassword", user.PasswordHash));
        }

        // --- TEST 10: SECUENCIA CORRECTA (UPDATE ANTES DE SAVE) ---
        [Fact]
        public async Task ChangePassword_ShouldCallUpdate_Before_SaveChanges()
        {
            // Arrange
            var useCase = new ChangePasswordAsync(_mockRepository.Object, _mockUnitOfWork.Object);
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("Old") };
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(user);

            var sequence = new MockSequence();
            _mockRepository.InSequence(sequence).Setup(r => r.Update(user));
            _mockUnitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync());

            // Act
            await useCase.Execute(1, new ChangePasswordInput { OldPassword = "Old", NewPassword = "New" });

            // Assert (Verificado implícitamente por MockSequence)
            _mockRepository.Verify(r => r.Update(user), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }


        // ==========================================
        //  GRUPO 3: ChangeUserEmail (ROBUSTEZ Y BORDES)
        // ==========================================

        // --- TEST 11: USUARIO NO ENCONTRADO (EMAIL) ---
        [Fact]
        public async Task ChangeEmail_ShouldThrowKeyNotFound_WhenUserDoesNotExist()
        {
            var useCase = new ChangeUserEmailAsync(_mockRepository.Object, _mockUnitOfWork.Object);
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((User?)null);

            var input = new ChangeEmailInput { NewEmail = "new@a.com", Password = "pass" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.Execute(1, input));
        }

        // --- TEST 12: ORDEN DE VALIDACIÓN (FAIL FAST) ---
        [Fact]
        public async Task ChangeEmail_ShouldCheckPassword_BEFORE_CheckingEmailUniqueness()
        {
            // Arrange
            var useCase = new ChangeUserEmailAsync(_mockRepository.Object, _mockUnitOfWork.Object);
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("RealPass") };

            // Configuramos que encuentre al usuario
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user);

            var input = new ChangeEmailInput { NewEmail = "taken@a.com", Password = "WrongPassword" };

            // Act & Assert
            // Debe fallar por contraseña (Unauthorized) NO por email duplicado (InvalidOperation)
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => useCase.Execute(1, input));

            // Si la contraseña está mal, no deberíamos perder tiempo buscando si el email está libre
            // Nota: Esto depende de cómo implementaste el SetupSequence o las llamadas. 
            // Si usas el mismo método FindAsync para ambos, este Assert es difícil de probar con Mocks simples,
            // pero garantiza que la excepción sea la de seguridad primero.
        }

        // --- TEST 13: CAMBIAR AL MISMO EMAIL (AUTO-CAMBIO) ---
        [Fact]
        public async Task ChangeEmail_ShouldAllowUpdate_IfNewEmailIsSameAsCurrent()
        {
            // Arrange
            var useCase = new ChangeUserEmailAsync(_mockRepository.Object, _mockUnitOfWork.Object);
            string myEmail = "me@test.com";
            var user = new User { Id = 1, Email = myEmail, PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("Pass") };

            // Mock inteligente:
            // 1. Encuentra usuario por ID
            // 2. Busca duplicados (email == myEmail && id != 1). Debería devolver NULL.
            _mockRepository.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user)
                .ReturnsAsync((User?)null);

            var input = new ChangeEmailInput { NewEmail = myEmail, Password = "Pass" };

            // Act
            await useCase.Execute(1, input);

            // Assert
            // No debe lanzar excepción. Se actualiza (aunque sea el mismo valor) y guarda.
            _mockRepository.Verify(r => r.Update(user), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // --- TEST 14: FALLO DE BASE DE DATOS (EMAIL) ---
        [Fact]
        public async Task ChangeEmail_ShouldPropagateException_WhenDatabaseExplodes()
        {
            var useCase = new ChangeUserEmailAsync(_mockRepository.Object, _mockUnitOfWork.Object);
            var user = new User { PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("Pass") };

            _mockRepository.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user)  // Encuentra user
                .ReturnsAsync((User?)null); // Email libre

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new TimeoutException("DB Down"));

            var input = new ChangeEmailInput { NewEmail = "new@test.com", Password = "Pass" };

            await Assert.ThrowsAsync<TimeoutException>(() => useCase.Execute(1, input));
        }

        // --- TEST 15: INTEGRIDAD (NO GUARDAR SI EMAIL OCUPADO) ---
        [Fact]
        public async Task ChangeEmail_ShouldNotCallSaveChanges_IfEmailIsTaken()
        {
            var useCase = new ChangeUserEmailAsync(_mockRepository.Object, _mockUnitOfWork.Object);
            var user = new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("Pass") };
            var otherUser = new User { Id = 2 };

            _mockRepository.SetupSequence(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(user)        // 1. Soy yo
                .ReturnsAsync(otherUser);  // 2. El email está ocupado

            var input = new ChangeEmailInput { NewEmail = "taken@test.com", Password = "Pass" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.Execute(1, input));

            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }
}