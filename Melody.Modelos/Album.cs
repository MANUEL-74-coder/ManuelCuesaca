using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Album
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public DateTime FechaLanzamiento { get; set; }
        public string PortadaUrl { get; set; }
        // Foreign keys
        public int GeneroId { get; set; }
        public string ArtistaId { get; set; }
        // Navigation properties
        public Usuario? Artista { get; set; }
        public Genero? Genero { get; set; }
        public List<Cancion>? Canciones { get; set; }
    }
}
