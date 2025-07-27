using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class CancionDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaLanzamiento { get; set; }
        public string ArchivoAudioUrl { get; set; } = string.Empty;
        public string? PortadaUrl { get; set; }
        public TimeSpan? Duracion { get; set; }
        public int GeneroId { get; set; }
        public string GeneroNombre { get; set; } = string.Empty;
        public int? AlbumId { get; set; }
        public string? AlbumNombre { get; set; }
        public int ArtistaId { get; set; }
        public string ArtistaNombre { get; set; } = string.Empty;
        public bool EsFavorito { get; set; } = false;
    }
}
