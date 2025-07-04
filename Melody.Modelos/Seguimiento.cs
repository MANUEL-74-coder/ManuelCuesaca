using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Seguimiento
    {
        public int Id { get; set; }
        public DateTime FechaSeguimiento { get; set; } = DateTime.Now;

        // Foreign key for the Follower
        public int UsuarioId { get; set; }
        public int ArtistaId { get; set; }

        // Navigation properties
        public Usuario? Usuario { get; set; }
        public Artista? Artista { get; set; }
    }
}
