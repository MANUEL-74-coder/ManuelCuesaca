using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>
{
        public AppDbContext (DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Melody.Modelos.Album> Albums { get; set; } = default!;

public DbSet<Melody.Modelos.Artista> Artistas { get; set; } = default!;

public DbSet<Melody.Modelos.Cancion> Canciones { get; set; } = default!;

public DbSet<Melody.Modelos.Genero> Generos { get; set; } = default!;

public DbSet<Melody.Modelos.Pago> Pagos { get; set; } = default!;

public DbSet<Melody.Modelos.Plan> Planes { get; set; } = default!;

public DbSet<Melody.Modelos.Playlist> Playlists { get; set; } = default!;

public DbSet<Melody.Modelos.PlaylistCancion> PlaylistsCanciones { get; set; } = default!;

public DbSet<Melody.Modelos.Seguimiento> Seguimientos { get; set; } = default!;

public DbSet<Melody.Modelos.Suscripcion> Suscripciones { get; set; } = default!;

    public DbSet<Melody.Modelos.Usuario> Usuarios { get; set; } = default!;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configurar relaciones personalizadas
        builder.Entity<Artista>()
            .HasOne(a => a.Usuario)
            .WithOne(u => u.Artista)
            .HasForeignKey<Artista>(a => a.UsuarioId);

        // Roles iniciales
        builder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int> { Id = 1, Name = "admin", NormalizedName = "ADMIN" },
            new IdentityRole<int> { Id = 2, Name = "artista", NormalizedName = "ARTISTA" },
            new IdentityRole<int> { Id = 3, Name = "userfree", NormalizedName = "USERFREE" },
            new IdentityRole<int> { Id = 4, Name = "userpremium", NormalizedName = "USERPREMIUM" }
        );
    }
}
