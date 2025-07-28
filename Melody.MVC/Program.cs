using Melody.API.Consumer;
using Melody.Modelos.DTOs;
using Melody.Modelos;
using Melody.MVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using static System.Net.WebRequestMethods;
using Melody.Modelos.DTO;

internal class Program
{
    private static void Main(string[] args)
    {
        Crud<AlbumDto>.Endpoint = "https://localhost:7115/api/Albums";
        Crud<Album>.Endpoint = "https://localhost:7115/api/Albums";
        Crud<Playlist>.Endpoint = "https://localhost:7115/api/Playlists";
        Crud<PlaylistDto>.Endpoint = "https://localhost:7115/api/Playlists";
        Crud<CancionDto>.Endpoint = "https://localhost:7115/api/Canciones";
        Crud<Genero>.Endpoint = "https://localhost:7115/api/Generos";
        Crud<Plan>.Endpoint = "https://localhost:7115/api/Planes";
        Crud<Pago>.Endpoint = "https://localhost:7115/api/Pagos";
        Crud<Suscripcion>.Endpoint = "https://localhost:7115/api/Suscripciones";
        Crud<EstadisticasSuscripcionesDto>.Endpoint = "https://localhost:7115/api/Suscripciones";
        Crud<SuscripcionAdminDto>.Endpoint = "https://localhost:7115/api/Suscripciones";
        Crud<PlaylistCancion>.Endpoint = "https://localhost:7115/api/PlaylistsCanciones";
        Crud<Seguimiento>.Endpoint = "https://localhost:7115/api/Seguimientos";
        Crud<Artista>.Endpoint = "https://localhost:7115/api/Artistas";
        Crud<ArtistaDto>.Endpoint = "https://localhost:7115/api/Artistas";
        Crud<MiPerfilDto>.Endpoint = "https://localhost:7115/api/Usuarios";
        Crud<UsuarioAdminDto>.Endpoint = "https://localhost:7115/api/Usuarios";
        Crud<Usuario>.Endpoint = "https://localhost:7115/api/Usuarios";
        Crud<MeGustaDto>.Endpoint = "https://localhost:7115/api/MeGusta";
        Crud<MeGusta>.Endpoint = "https://localhost:7115/api/MeGusta";
        Crud<CapturarPagoResponseDto>.Endpoint = "https://localhost:7115/api/Pagos";
        Crud<CrearOrdenResponseDto>.Endpoint = "https://localhost:7115/api/Pagos";
        Crud<MiembrosFamiliaResponseDto>.Endpoint = "https://localhost:7115/api/Suscripciones";
        Crud<MiSuscripcionResponseDto>.Endpoint = "https://localhost:7115/api/Suscripciones";
        //Esto lo uso para mostrar los followers del artista
        Crud<object>.Endpoint = "https://localhost:7115/api/Seguimientos";
        //Crud<dynamic>.Endpoint = "https://localhost:7115/api/Pagos";

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddHttpClient();

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
          .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
          {
              options.LoginPath = "/Auth/Login";
              options.LogoutPath = "/Auth/Salir";
              options.AccessDeniedPath = "/Auth/AccessDenied";
              options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
              options.SlidingExpiration = true;
          });

        // Configurar sesiones (IMPORTANTE para guardar JWT)
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(60); // Mismo tiempo que JWT
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // Registrar HttpContextAccessor (necesario para AuthService)
        builder.Services.AddHttpContextAccessor();

        // Registrar el AuthService
        builder.Services.AddScoped<AuthService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Auth}/{action=Login}/{id?}");

        app.Run();
    }
}