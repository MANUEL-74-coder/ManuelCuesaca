using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class AlbumDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaLanzamiento { get; set; }
        public string? PortadaUrl { get; set; }
        public int ArtistaId { get; set; }
        public string NombreArtista { get; set; } = string.Empty;
        public int GeneroId { get; set; }
        public string GeneroNombre { get; set; } = string.Empty;
        public int TotalCanciones { get; set; }
        public List<CancionDto>? Canciones { get; set; }
    }
}