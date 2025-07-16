using Melody.API.Consumer;
using Melody.Modelos;
using Melody.MVC.Services;
using static System.Net.WebRequestMethods;

internal class Program
{
    private static void Main(string[] args)
    {
        Crud<Album>.Endpoint = "https://localhost:7115/api/Albums";
        Crud<Playlist>.Endpoint = "https://localhost:7115/api/Playlists";
        Crud<Cancion>.Endpoint = "https://localhost:7115/api/Canciones";
        Crud<Genero>.Endpoint = "https://localhost:7115/api/Generos";
        Crud<Plan>.Endpoint = "https://localhost:7115/api/Planes";
        Crud<Pago>.Endpoint = "https://localhost:7115/api/Pagos";
        Crud<Suscripcion>.Endpoint = "https://localhost:7115/api/Suscripciones";
        Crud<PlaylistCancion>.Endpoint = "https://localhost:7115/api/PlaylistsCanciones";
        Crud<Seguimiento>.Endpoint = "https://localhost:7115/api/Seguimientos";



        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

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

        builder.Services.AddHttpClient();
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

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Auth}/{action=Login}/{id?}");

        app.Run();
    }
}