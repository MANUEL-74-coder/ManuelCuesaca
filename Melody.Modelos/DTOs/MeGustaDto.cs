using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class MeGustaDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int CancionId { get; set; }
        public DateTime FechaAgregado { get; set; }

        // Información de la canción
        public string CancionTitulo { get; set; } = string.Empty;
        public string ArchivoAudioUrl { get; set; } = string.Empty;
        public string? PortadaUrl { get; set; }
        public TimeSpan? Duracion { get; set; }

        // Información del artista
        public int ArtistaId { get; set; }
        public string ArtistaNombre { get; set; } = string.Empty;

        // Información del álbum/género
        public string? AlbumNombre { get; set; }
        public string GeneroNombre { get; set; } = string.Empty;
    }
}