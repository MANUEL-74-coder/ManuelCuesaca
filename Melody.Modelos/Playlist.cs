using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Playlist
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Imagen { get; set; }
        public bool EsPublica { get; set; }
        // Foreign key
        public string UsuarioId { get; set; }
        // Navigation properties
        public Usuario? Usuario { get; set; }
        public List<PlaylistCancion>? PlaylistCanciones { get; set; } = new();
    }
}
