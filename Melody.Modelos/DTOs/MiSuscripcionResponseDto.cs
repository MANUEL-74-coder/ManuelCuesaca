using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class MiSuscripcionResponseDto
    {
        public bool TienesSuscripcion { get; set; }
        public bool EsPropietario { get; set; }
        public SuscripcionInfoDto? Suscripcion { get; set; }
    }
}