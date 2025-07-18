using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Melody.Modelos.DTOs
{
    public class CrearPlaylistDto
    {
        [Required(ErrorMessage = "El nombre de la playlist es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string Nombre { get; set; }

        public bool EsPublica { get; set; } = false;

        public IFormFile? Imagen { get; set; }
    }
}