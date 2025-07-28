using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class SuscripcionInfoDto
    {
        public int Id { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool EsActiva { get; set; }
        public string PlanNombre { get; set; } = string.Empty;
        public int PlanNumeroUsuarios { get; set; }
        public double PlanPrecio { get; set; }
        public int DiasRestantes { get; set; }
        public int? MiembrosActivos { get; set; }
        public int? TotalUsuarios { get; set; }
        public int? EspaciosDisponibles { get; set; }
        public string? PropietarioNombre { get; set; }
        public string? PropietarioEmail { get; set; }
    }
}
