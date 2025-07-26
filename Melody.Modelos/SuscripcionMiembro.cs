using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class SuscripcionMiembro
    {
        public int Id { get; set; }
        public int SuscripcionId { get; set; }  // Referencia a la suscripción principal
        public int UsuarioId { get; set; }      // Usuario que se une a la familia
        public DateTime FechaUnion { get; set; } = DateTime.Now;
        public bool EsActivo { get; set; } = true;
        public string? Rol { get; set; }        // "Principal", "Miembro"

        // Navigation properties
        public Suscripcion? Suscripcion { get; set; }
        public Usuario? Usuario { get; set; }
    }
}