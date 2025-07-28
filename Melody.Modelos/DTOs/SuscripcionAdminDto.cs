using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class SuscripcionAdminDto
    {
        public int Id { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool EsActiva { get; set; }
        public string UsuarioEmail { get; set; } = "";
        public string UsuarioNombre { get; set; } = "";
        public string PlanNombre { get; set; } = "";
    }
}
