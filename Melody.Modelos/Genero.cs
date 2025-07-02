using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Genero
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        // Navigation properties
        public List<Album>? Albums { get; set; }
        public List<Cancion>? Canciones { get; set; }
    }
}
