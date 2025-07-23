using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Melody.API.Services
{
    public class VerificarSuscripcionesService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VerificarSuscripcionesService> _logger;

        public VerificarSuscripcionesService(IServiceProvider serviceProvider, ILogger<VerificarSuscripcionesService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await VerificarSuscripciones();
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Esperar 1 día
            }
        }

        private async Task VerificarSuscripciones()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();

            // Buscar suscripciones vencidas
            var vencidas = await context.Suscripciones
                .Include(s => s.Usuario)
                .Where(s => s.EsActiva && s.FechaFin <= DateTime.Now)
                .ToListAsync();

            foreach (var suscripcion in vencidas)
            {
                // Desactivar suscripción
                suscripcion.EsActiva = false;

                // Cambiar rol a free
                var usuario = suscripcion.Usuario;
                await userManager.RemoveFromRoleAsync(usuario, "userpremium");
                await userManager.AddToRoleAsync(usuario, "userfree");

                _logger.LogInformation("Suscripción vencida para {Email}", usuario.Email);
            }

            await context.SaveChangesAsync();
        }
    }
}