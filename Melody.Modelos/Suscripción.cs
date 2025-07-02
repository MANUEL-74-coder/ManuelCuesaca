using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Suscripcion
    {
        public int Id { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        // Foreign keys
        public string UsuarioId { get; set; }
        public int PlanId { get; set; }
        // Navigation properties
        public Usuario? Usuario { get; set; }
        public Plan? Plan { get; set; }
        public List<Pago>? Pagos { get; set; }
    }
}
