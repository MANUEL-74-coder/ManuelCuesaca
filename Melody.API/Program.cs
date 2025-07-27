using System.Text;
using Melody.API.Service;
using Melody.Modelos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Melody.API.Inicializador;
using Meldoy.API.Inicializador;
using Melody.API.Services;
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("AppDbContext");
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
        );

        //Configuración de Identity
        builder.Services.AddIdentity<Usuario, IdentityRole<int>>(options =>
        {
            // Configuración de contraseñas
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            // Configuración de lockout o intentos fallidos 
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // Configuración de usuario
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true; // Requerir confirmación por email
        })
        .AddEntityFrameworkStores<AppDbContext>()  // ⭐ ESTO REGISTRA UserManager, SignInManager, etc.
        .AddDefaultTokenProviders();


        //Tiempo de vida de los tokens
        builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
        {

            options.TokenLifespan = TimeSpan.FromMinutes(30);
        });

        // Configuración JWT
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT Key no está configurada en appsettings.json");
        }

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        // Registrar servicios personalizados
        builder.Services.AddScoped<JwtService>();
        builder.Services.AddScoped<EmailService>();
        builder.Services.AddScoped<IDbInicializador, DbInicializador>();
        builder.Services.AddScoped<IAzureBlobService, AzureBlobService>();
        builder.Services.AddScoped<IUsuarioService, UsuarioService>();
        builder.Services.AddScoped<IAudioService, AudioService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IPayPalService, PayPalService>();
        builder.Services.AddHostedService<VerificarSuscripcionesService>();
        builder.Services.AddScoped<IPdfService, PdfService>();


        //Add services to the container
        builder.Services
            .AddControllers()
            .AddNewtonsoftJson(
                options => options.SerializerSettings.ReferenceLoopHandling
                = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

        // Configuración de CORS para permitir llamadas desde el MVC
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Melody API", Version = "v1" });

            // Configuración para JWT en Swagger
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });
        });
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors(c => c.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
        app.UseAuthentication();
        app.UseAuthorization();

        //Aplicar migraciones y datos iniciales
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            try
            {
                var inicializador = services.GetRequiredService<IDbInicializador>();
                inicializador.InicializarAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger<Program>();
                logger.LogError(ex, "Un Error ocurrió al ejecutar la migración");
            }
        }
        app.MapControllers();

        app.Run();
    }
}