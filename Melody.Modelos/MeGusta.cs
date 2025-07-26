using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class MeGusta
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int CancionId { get; set; }
        public DateTime FechaAgregado { get; set; } = DateTime.Now;

        // Navigation properties
        public Usuario? Usuario { get; set; }
        public Cancion? Cancion { get; set; }
    }
}