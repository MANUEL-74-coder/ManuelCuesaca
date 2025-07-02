using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Pago
    {
        public int Id { get; set; }
        public double Monto { get; set; }
        public DateTime FechaPago { get; set; } = DateTime.Now;
        public string MetodoPago { get; set; }
        // Foreign keys
        public int SuscripcionId { get; set; }
        // Navigation properties
        public Suscripcion? Suscripcion { get; set; }
    }
}
