using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Melody.Modelos.DTOs
{
    public class ActualizarPerfilUsuarioDto
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public IFormFile? FotoPerfil { get; set; }
    }
}