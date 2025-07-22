using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class MiPerfilDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

        // Campos planos de suscripción
        public bool TieneSuscripcionActiva { get; set; }
        public string? PlanNombre { get; set; }
        public double? PlanPrecio { get; set; }
        public DateTime? SuscripcionFechaInicio { get; set; }
        public DateTime? SuscripcionFechaFin { get; set; }
    }
}