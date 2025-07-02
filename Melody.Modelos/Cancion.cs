using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Cancion
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public DateTime FechaLanzamiento { get; set; }
        public string ArchivoAudio { get; set; }
        public string PortadaUrl { get; set; }

        // Foreign keys
        public int? AlbumId { get; set; }
        public int GeneroId { get; set; }
        public string ArtistaId { get; set; }
        // Navigation properties
        public Usuario? Artista { get; set; }
        public Album? Album { get; set; }
        public Genero? Genero { get; set; }
        public List<PlaylistCancion>? PlaylistCanciones { get; set; }
    }
}
