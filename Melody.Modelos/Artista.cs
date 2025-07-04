using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Artista
    {
        public int Id { get; set; }
        public string NombreArtista { get; set; }
        public string? Biografia { get; set; }
        public string? ImagenPerfil { get; set; }

        // Foreign key al Usuario
        public int UsuarioId { get; set; }

        // Navigation properties
        public Usuario? Usuario { get; set; }
        public List<Album>? Albums { get; set; }
        public List<Cancion>? Canciones { get; set; }
        public List<Seguimiento>? Seguidores { get; set; }
    }
}
