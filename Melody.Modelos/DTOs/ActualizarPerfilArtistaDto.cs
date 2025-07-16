using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Melody.Modelos.DTOs
{
    public class ActualizarPerfilArtistaDto
    {
        public string NombreArtista { get; set; } = string.Empty;
        public string? Biografia { get; set; }
        public IFormFile? ImagenPerfil { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public IFormFile? FotoPerfil { get; set; }
    }
}