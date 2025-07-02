using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos
{
    public class Plan
    {
        public int Id { get; set; }
        public string Nombre { get; set; }       
        public string Descripcion { get; set; }  
        public double Precio { get; set; }      
        public int DuracionDias { get; set; }  

        public List<Suscripcion>? Suscripciones { get; set; }
    }
}
