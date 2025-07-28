using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class EstadisticasSuscripcionesDto
    {
        // Suscripciones
        public int TotalActivas { get; set; }
        public int TotalHistoricas { get; set; }

        // Usuarios
        public int UsuariosPremium { get; set; }

        // CAMBIAR: MiembrosFamiliares → UsuariosAdicionales
        public int UsuariosAdicionales { get; set; }

        // Ingresos
        public decimal IngresosTotales { get; set; }
        public decimal IngresosEsteMes { get; set; }

        // Planes más populares
        public List<PlanPopularDto> PlanesMasUsados { get; set; } = new();
    }
}
