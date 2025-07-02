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
        // Foreign key for the Follower
        public string UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        //Foreign key for the Artist being followed
        public string ArtistaId { get; set; }
        public Usuario? Artista { get; set; }
        public DateTime FechaSeguimiento { get; set; } = DateTime.Now;
    }
}
