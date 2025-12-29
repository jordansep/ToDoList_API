using Moq;
using System.Linq.Expressions;
using ToDoList_Core.Domain.Enums;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Implementation;
using ToDoList_Core.Services.Interfaces;
using Xunit;

namespace ToDoList.Tests
{
    public class DutyServiceTests
    {
        private readonly Mock<IRepository<Duty>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly DutyService _dutyService;

        public DutyServiceTests()
        {
            // ARRANGE COMÚN
            _mockRepository = new Mock<IRepository<Duty>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _dutyService = new DutyService(_mockRepository.Object, _mockUnitOfWork.Object);
        }

        // ==========================================
        //  TESTS DE "CAMINO FELIZ" (HAPPY PATH)
        // ==========================================

        [Fact]
        public async Task CreateDuty_ShouldCallAddAndSaveChanges_AndAssignUserId()
        {
            // Arrange
            var newDuty = new Duty { HeadLine = "Test Duty" };
            int userId = 99;

            // Act
            await _dutyService.CreateDuty(newDuty, userId);

            // Assert
            Assert.Equal(userId, newDuty.UserID);
            _mockRepository.Verify(r => r.Add(newDuty), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetDutiesForUserAsync_ShouldReturnDuties_WhenTheyExist()
        {
            // Arrange
            int userId = 5;
            var fakeList = new List<Duty>
            {
                new Duty { Id = 1, UserID = userId, HeadLine = "Tarea 1" },
                new Duty { Id = 2, UserID = userId, HeadLine = "Tarea 2" }
            };

            _mockRepository.Setup(r => r.FindAllAsync(
                It.IsAny<Expression<Func<Duty, bool>>>(),
                It.IsAny<Expression<Func<Duty, object>>[]>()
            )).ReturnsAsync(fakeList);

            // Act
            var result = await _dutyService.GetDutiesForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task FindDuty_ShouldReturnDuty_WhenItExists()
        {
            // Arrange
            var expectedDuty = new Duty { Id = 10, HeadLine = "Encontrada" };
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Duty, bool>>>()))
                .ReturnsAsync(expectedDuty);

            // Act
            var result = await _dutyService.FindDuty(d => d.Id == 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Encontrada", result.HeadLine);
        }

        [Fact]
        public async Task UpdateDutyAsync_ShouldCallUpdateAndSaveChanges()
        {
            // Arrange
            var dutyToUpdate = new Duty { Id = 1, HeadLine = "Editada" };

            // Act
            await _dutyService.UpdateDutyAsync(dutyToUpdate);

            // Assert
            _mockRepository.Verify(r => r.Update(dutyToUpdate), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteDuty_ShouldCallDeleteAndSaveChanges()
        {
            // Arrange
            var dutyToDelete = new Duty { Id = 666 };

            // Act
            await _dutyService.DeleteDuty(dutyToDelete);

            // Assert
            _mockRepository.Verify(r => r.Delete(dutyToDelete), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==========================================
        //  GRUPO 2: TESTS DE LÓGICA Y BORDES
        // ==========================================

        // --- TEST 6: BUSCAR Y NO ENCONTRAR ---
        [Fact]
        public async Task FindDuty_ShouldReturnNull_WhenDutyDoesNotExist()
        {
            // Arrange
            // Configuramos el mock para que devuelva 'null'
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Duty, bool>>>()))
                .ReturnsAsync((Duty?)null);

            // Act
            var result = await _dutyService.FindDuty(d => d.Id == 999);

            // Assert
            // Confirmamos que el servicio maneja el null correctamente y no explota
            Assert.Null(result);
        }

        // --- TEST 7: LISTA VACÍA ---
        [Fact]
        public async Task GetDutiesForUserAsync_ShouldReturnEmptyList_WhenUserHasNoDuties()
        {
            // Arrange
            _mockRepository.Setup(r => r.FindAllAsync(
                It.IsAny<Expression<Func<Duty, bool>>>(),
                It.IsAny<Expression<Func<Duty, object>>[]>()
            )).ReturnsAsync(new List<Duty>()); // Lista vacía

            // Act
            var result = await _dutyService.GetDutiesForUserAsync(1);

            // Assert
            Assert.NotNull(result); // No debe ser null
            Assert.Empty(result);   // Debe estar vacía
        }

        // --- TEST 8: LÓGICA DE FÁBRICA (BuildDuty) ---
        [Fact]
        public void BuildDuty_ShouldCreateValidInstance_WithCurrentDate()
        {
            // Arrange
            string title = "Nueva Tarea";
            string desc = "Descripción de prueba";

            // Act
            var result = _dutyService.BuildDuty(title, desc);

            // Assert
            Assert.Equal(title, result.HeadLine);
            Assert.Equal(desc, result.Description);
            // Verificamos que la fecha de inicio sea "ahora" (con margen de 1 segundo)
            Assert.True((DateTime.Now - result.StartDate).TotalSeconds < 1);
        }

        // --- TEST 9: ERROR DE BASE DE DATOS (EN GUARDADO) ---
        [Fact]
        public async Task CreateDuty_ShouldThrowException_WhenDatabaseFails()
        {
            // Arrange
            var newDuty = new Duty();
            // Simulamos que la base de datos lanza una excepción al guardar
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Exception("Error de conexión SQL"));

            // Act & Assert
            // Verificamos que la excepción "suba" y no sea tragada silenciosamente
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _dutyService.CreateDuty(newDuty, 1));

            Assert.Equal("Error de conexión SQL", exception.Message);
        }

        // ==========================================
        //  GRUPO 3: NUEVOS TESTS (ROBUSTEZ Y FALLOS)
        // ==========================================

        // --- TEST 10: ERROR DE REPOSITORIO (EN BÚSQUEDA) ---
        [Fact]
        public async Task GetDutiesForUserAsync_ShouldThrowException_WhenRepositoryFails()
        {
            // Arrange
            _mockRepository.Setup(r => r.FindAllAsync(
                It.IsAny<Expression<Func<Duty, bool>>>()
            )).ThrowsAsync(new InvalidOperationException("Error crítico"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _dutyService.GetDutiesForUserAsync(1));
        }
        // --- TEST 11: VERIFICAR VALORES POR DEFECTO ---
        [Fact]
        public void BuildDuty_ShouldSetStatusToToDo_ByDefault()
        {
            // Queremos asegurar que una tarea nueva siempre nace como "ToDo" (0)
            // y no con otro estado accidentalmente.
            var result = _dutyService.BuildDuty("Test", "Test");

            Assert.Equal(DutyStatus.ToDo, result.Status);
        }

        // --- TEST 12: INTEGRIDAD TRANSACCIONAL (Create) ---
        [Fact]
        public async Task CreateDuty_ShouldNotCallSaveChanges_IfRepositoryAddFails()
        {
            // Arrange
            var newDuty = new Duty();
            // Simulamos que el repositorio falla al intentar añadir (ej: ID duplicado en memoria)
            _mockRepository.Setup(r => r.Add(It.IsAny<Duty>()))
                .Throws(new InvalidOperationException("Fallo al añadir"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _dutyService.CreateDuty(newDuty, 1));

            // Verificamos que NUNCA se intentó guardar cambios si el paso anterior falló
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        // --- TEST 13: MANEJO DE ERRORES (Update) ---
        [Fact]
        public async Task UpdateDutyAsync_ShouldThrowException_WhenDatabaseFails()
        {
            // Arrange
            var duty = new Duty();
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Exception("Error SQL en Update"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _dutyService.UpdateDutyAsync(duty));

            Assert.Equal("Error SQL en Update", exception.Message);
        }

        // --- TEST 14: MANEJO DE ERRORES (Delete) ---
        [Fact]
        public async Task DeleteDuty_ShouldThrowException_WhenDatabaseFails()
        {
            // Arrange
            var duty = new Duty();
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Exception("Error SQL en Delete"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _dutyService.DeleteDuty(duty));

            Assert.Equal("Error SQL en Delete", exception.Message);
        }

        // --- TEST 15: MANEJO DE ERRORES (Find) ---
        [Fact]
        public async Task FindDuty_ShouldThrowException_WhenRepositoryFails()
        {
            // Arrange
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Duty, bool>>>()))
                .ThrowsAsync(new TimeoutException("Base de datos no responde"));

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() =>
                _dutyService.FindDuty(d => d.Id == 1));
        }
        // ==========================================
        //  GRUPO 4: TESTS DE FILTRADO REAL (LÓGICA LAMBDA)
        // ==========================================

        // --- TEST 16: VERIFICAR QUE EL FILTRO DE USUARIO FUNCIONA ---
        [Fact]
        public async Task GetDutiesForUserAsync_ShouldFilterCorrectly_ByUserId()
        {
            // Arrange
            int targetUser = 1;
            int otherUser = 2;

            // Creamos una "Base de datos en memoria" con datos mezclados
            var dbData = new List<Duty>
            {
                new Duty { Id = 1, UserID = targetUser, HeadLine = "Mía 1" },
                new Duty { Id = 2, UserID = otherUser, HeadLine = "De otro" }, // No debería salir
                new Duty { Id = 3, UserID = targetUser, HeadLine = "Mía 2" }
            };

            // MAGIA: Le enseñamos al Mock a EJECUTAR la lambda que recibe
            _mockRepository.Setup(r => r.FindAllAsync(
                It.IsAny<Expression<Func<Duty, bool>>>(),
                It.IsAny<Expression<Func<Duty, object>>[]>()
            )).ReturnsAsync((Expression<Func<Duty, bool>> predicate, Expression<Func<Duty, object>>[] includes) =>
            {
                // Compilamos y ejecutamos el filtro real contra nuestra lista falsa
                return dbData.Where(predicate.Compile()).ToList();
            });

            // Act
            var result = await _dutyService.GetDutiesForUserAsync(targetUser);

            // Assert
            Assert.Equal(2, result.Count()); // Solo debe traer las 2 del targetUser
            Assert.DoesNotContain(result, d => d.UserID == otherUser); // Asegura que no hay intrusos
        }

        // ==========================================
        //  GRUPO 5: TESTS DE SECUENCIA (ORDEN DE LLAMADAS)
        // ==========================================

        // --- TEST 17: ORDEN EN CREAR ---
        [Fact]
        public async Task CreateDuty_ShouldCallAdd_BEFORE_SaveChanges()
        {
            // Arrange
            var duty = new Duty();
            var sequence = new MockSequence(); // Herramienta para verificar orden

            // Configuramos que esperamos Add y luego Save en ese orden
            _mockRepository.InSequence(sequence).Setup(r => r.Add(duty));
            _mockUnitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync());

            // Act
            await _dutyService.CreateDuty(duty, 1);

            // Assert
            // Si SaveChanges se llamara antes de Add, esto fallaría.
            _mockRepository.Verify(r => r.Add(duty), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // --- TEST 18: ORDEN EN ACTUALIZAR ---
        [Fact]
        public async Task UpdateDutyAsync_ShouldCallUpdate_BEFORE_SaveChanges()
        {
            var duty = new Duty();
            var sequence = new MockSequence();

            _mockRepository.InSequence(sequence).Setup(r => r.Update(duty));
            _mockUnitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync());

            await _dutyService.UpdateDutyAsync(duty);

            _mockRepository.Verify(r => r.Update(duty), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // --- TEST 19: ORDEN EN BORRAR ---
        [Fact]
        public async Task DeleteDuty_ShouldCallDelete_BEFORE_SaveChanges()
        {
            var duty = new Duty();
            var sequence = new MockSequence();

            _mockRepository.InSequence(sequence).Setup(r => r.Delete(duty));
            _mockUnitOfWork.InSequence(sequence).Setup(u => u.SaveChangesAsync());

            await _dutyService.DeleteDuty(duty);

            _mockRepository.Verify(r => r.Delete(duty), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // ==========================================
        //  GRUPO 6: ROBUSTEZ Y VALIDACIÓN EXTRA
        // ==========================================

        // --- TEST 20: CREATE CON DUTY NULO ---
        [Fact]
        public async Task CreateDuty_ShouldThrowNullReference_WhenDutyIsNull()
        {
            // Intentar asignar UserID a un objeto null debería lanzar excepción
            await Assert.ThrowsAsync<NullReferenceException>(() =>
                _dutyService.CreateDuty(null!, 1));
        }

        // --- TEST 21: CONCURRENCIA EN UPDATE ---
        [Fact]
        public async Task UpdateDutyAsync_ShouldPropagate_ConcurrencyException()
        {
            // Simulamos que otro usuario modificó el registro al mismo tiempo
            var duty = new Duty();
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Conflicto de concurrencia", new List<Microsoft.EntityFrameworkCore.Update.IUpdateEntry>()));

            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() =>
                _dutyService.UpdateDutyAsync(duty));
        }

        // --- TEST 22: RESTRICCIÓN EN DELETE (Ej: Llave foránea) ---
        [Fact]
        public async Task DeleteDuty_ShouldPropagate_DbUpdateException()
        {
            var duty = new Duty();
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Error de restricción FK"));

            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
                _dutyService.DeleteDuty(duty));
        }

        // --- TEST 23: BUILD DUTY CON DESCRIPCIÓN NULA ---
        [Fact]
        public void BuildDuty_ShouldAllowNullDescription()
        {
            // Arrange
            string title = "Titulo";
            string? desc = null; // Descripción opcional

            // Act
            var result = _dutyService.BuildDuty(title, desc);

            // Assert
            Assert.Equal(title, result.HeadLine);
            Assert.Null(result.Description); // Debe permitir null
            Assert.Equal(DutyStatus.ToDo, result.Status);
        }

        // --- TEST 24: BUILD DUTY CON CADENAS VACÍAS ---
        [Fact]
        public void BuildDuty_ShouldHandleEmptyStrings()
        {
            var result = _dutyService.BuildDuty("", "");

            Assert.Equal("", result.HeadLine);
            Assert.Equal("", result.Description);
            Assert.NotNull(result);
        }

        // --- TEST 25: FIND DUTY LLAMA AL REPOSITORIO CON PREDICADO ---
        [Fact]
        public async Task FindDuty_ShouldPassPredicateToRepository()
        {
            // Arrange
            _mockRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Duty, bool>>>()))
                .ReturnsAsync(new Duty());

            // Act
            await _dutyService.FindDuty(d => d.Id == 123);

            // Assert
            // Verificamos que FindAsync fue llamado con ALGUNA expresión
            _mockRepository.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Duty, bool>>>()), Times.Once);
        }
    }
}