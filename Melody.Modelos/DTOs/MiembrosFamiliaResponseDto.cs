using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class MiembrosFamiliaResponseDto
    {
        public string UsuarioPrincipalEmail { get; set; } = string.Empty;
        public string UsuarioPrincipalNombre { get; set; } = string.Empty;
        public List<MiembroFamiliarDto> Miembros { get; set; } = new();
        public int TotalUsuarios { get; set; }
        public int EspaciosDisponibles { get; set; }
        public bool PuedeAgregarMas { get; set; }
    }
}