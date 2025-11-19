using Microsoft.EntityFrameworkCore;
using ToDoList_Core.Services.Implementation;
using ToDoList_Core.Services.Interfaces;
using ToDoList_Infrastructure.Server.Implementation;
using ToDoListAPI.Mapping;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ToDoList_API.Authorization.Rule;
using ToDoList_API.Authorization.RuleHandler;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi;
using ToDoList.Core.Domain.UseCases.Implementation;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- ZONA DE REGISTRO DE SERVICIOS (DI) ---

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(doc =>
        {
            var securityScheme = new OpenApiSecuritySchemeReference("Bearer");
            var requirement = new OpenApiSecurityRequirement();
            requirement[securityScheme] = new List<string>();
            return requirement;
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
    builder.Services.AddScoped<ChangePasswordAsync>();
    builder.Services.AddScoped<ChangeUserEmailAsync>();
    
    // Registrar el Authorization Handler
    builder.Services.AddScoped<IAuthorizationHandler, IsOwnerOrAdminHandler>();
    builder.Services.AddScoped<IAuthorizationHandler, IsDutyOwnerOrAdminHandler>();

    // Configuración de Autenticación JWT
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var tokenKey = builder.Configuration.GetSection("AppSettings:Token").Value
                ?? throw new InvalidOperationException("La clave JWT no está configurada en appsettings.json");

            if (tokenKey.Length < 64)
            {
                throw new ArgumentException("La clave JWT debe tener al menos 64 caracteres.");
            }

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            // DEBUGGING: Eventos para ver que esta pasando con el token
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"[JWT ERROR] Authentication Failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine("[JWT SUCCESS] Token validado correctamente");
                    var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
                    Console.WriteLine($"[JWT CLAIMS] {string.Join(", ", claims ?? Array.Empty<string>())}");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    Console.WriteLine($"[JWT CHALLENGE] Error: {context.Error}, Description: {context.ErrorDescription}");
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    Console.WriteLine($"[JWT MESSAGE] Header: {authHeader ?? "NONE"}");
                    return Task.CompletedTask;
                }
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("IsOwnerOrAdmin", policy =>
            policy.AddRequirements(new IsOwnerOrAdminRequirement())
        );
        options.AddPolicy("IsDutyOwnerOrAdmin", policy =>
            policy.AddRequirements(new IsDutyOwnerOrAdminRequirement())
        );
    });
    builder.Services.AddHttpContextAccessor();
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