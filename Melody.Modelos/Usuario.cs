using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Melody.Modelos
{
    public class Usuario : IdentityUser<int>
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string? FotoPerfil { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        // Navigation properties
        public List<Playlist>? Playlists { get; set; }
        public List<Suscripcion>? Suscripciones { get; set; }
        public List<Seguimiento>? Seguimientos { get; set; }

        // Relación con Artista
        public Artista? Artista { get; set; }
    }
}
