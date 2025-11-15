using Microsoft.EntityFrameworkCore;
using ToDoList_Core.Services.Implementation;
using ToDoList_Core.Services.Interfaces;
using ToDoList_Infrastructure.Server.Implementation;
using ToDoListAPI.Mapping;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ToDoList_API.Authorization.Rule;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- ZONA DE REGISTRO DE SERVICIOS (DI) ---

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
      options.AddSecurityDefinition(name: JwtBearerDefaults.AuthenticationScheme,
         securityScheme: new OpenApiSecurityScheme { 
           Name = "Authorization",
            Description = "Enter the Bearer Authorization: ´Bearer Generated´",
     In = ParameterLocation.Header,
      Type = SecuritySchemeType.Http,
       Scheme = "Bearer",
            BearerFormat = "JWT"
       });

        options.AddSecurityRequirement(doc =>
        {
   var securityRequirement = new OpenApiSecurityRequirement();
   securityRequirement.Add(
  new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme),
    new List<string>()
      );
  return securityRequirement;
        });
    });

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

    // Configuración de Autenticación JWT
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Valida el broche usando la misma clave secreta
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    builder.Configuration.GetSection("AppSettings:Token").Value
                )),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("IsOwnerOrAdmin", policy =>
        policy.AddRequirements(new IsOwnerOrAdminRequirement())
        );
    });
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