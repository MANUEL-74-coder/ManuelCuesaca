using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class RegistroDto
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmarPassword { get; set; }
        public bool EsArtista { get; set; } = false;
    }
}
