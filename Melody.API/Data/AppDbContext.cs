using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
public class AppDbContext : IdentityDbContext<Usuario,IdentityRole<int>,int>
{

    public AppDbContext (DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

public DbSet<Melody.Modelos.Album> Albums { get; set; } = default!;

public DbSet<Melody.Modelos.Cancion> Canciones { get; set; } = default!;

public DbSet<Melody.Modelos.Genero> Generos { get; set; } = default!;

public DbSet<Melody.Modelos.Pago> Pagos { get; set; } = default!;

public DbSet<Melody.Modelos.Plan> Planes { get; set; } = default!;

public DbSet<Melody.Modelos.Playlist> Playlists { get; set; } = default!;

public DbSet<Melody.Modelos.PlaylistCancion> PlaylistsCanciones { get; set; } = default!;

public DbSet<Melody.Modelos.Seguimiento> Seguimientos { get; set; } = default!;

public DbSet<Melody.Modelos.Suscripcion> Suscripciones { get; set; } = default!;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configuraciones personalizadas
        builder.Entity<PlaylistCancion>()
            .HasKey(pc => new { pc.PlaylistId, pc.CancionId }); // Clave compuesta

        builder.Entity<Seguimiento>()
            .HasKey(s => new { s.UsuarioId, s.ArtistaId }); // Clave compuesta

        // Opcional: Configurar eliminación en cascada para relaciones
        builder.Entity<Usuario>()
            .HasMany(u => u.Albums)
            .WithOne(a => a.Artista)
            .OnDelete(DeleteBehavior.Restrict); // Evita eliminación en cascada
    }
}
