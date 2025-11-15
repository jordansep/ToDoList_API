using Microsoft.EntityFrameworkCore;
using ToDoList_Core.Services.Implementation;
using ToDoList_Core.Services.Interfaces;
using ToDoList_Infrastructure.Server.Implementation;
using ToDoListAPI.Mapping;
using Microsoft.IdentityModel.Tokens;
using System.Text;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- ZONA DE REGISTRO DE SERVICIOS (DI) ---

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // DB Context
    builder.Services.AddDbContext<AppDBContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")
            )
        );


    // AutoMapper 
    builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

    // Inyección de Dependencias
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IDutyService, DutyService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    // Falta la configuración de JWT

    var app = builder.Build();

    // --- PIPELINE DE MIDDLEWARE ---

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error Msg: {ex.Message}");
    Console.WriteLine($"StackTrace: {ex.StackTrace}");
}