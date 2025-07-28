using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class UsuarioAdminDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FotoPerfil { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new();

        // Para vista detalle 
        public List<SuscripcionInfoDto>? Suscripciones { get; set; }
    }
}
