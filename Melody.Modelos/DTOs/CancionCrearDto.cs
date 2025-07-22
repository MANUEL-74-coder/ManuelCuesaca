using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Melody.Modelos.DTOs
{
    public class CancionCrearDto
    {
        [Required(ErrorMessage = "El título es requerido")]
        public string Titulo { get; set; } = string.Empty;
        [Required(ErrorMessage = "El género es requerido")]
        public int GeneroId { get; set; }

        public int? AlbumId { get; set; }
        public DateTime FechaLanzamiento { get; set; } = DateTime.Now;
        [Required(ErrorMessage = "El archivo de audio es requerido")]
        public IFormFile ArchivoAudio { get; set; } = null!;
        public IFormFile? ImagenPortada { get; set; }
    }
}