using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Melody.Modelos.DTOs
{
    public class ActualizarCancionDto
    {
        public string? Titulo { get; set; } = string.Empty;
        public int GeneroId { get; set; }

        public int? AlbumId { get; set; }
        public DateTime FechaLanzamiento { get; set; } = DateTime.Now;
        public IFormFile? ArchivoAudio { get; set; } = null!;
        public IFormFile? ImagenPortada { get; set; }
    }
}