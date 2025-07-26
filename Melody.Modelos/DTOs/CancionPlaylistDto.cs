using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class CancionPlaylistDto
    {
        public int PlaylistCancionId { get; set; } // Para poder eliminar de la playlist
        public int CancionId { get; set; }
        public string CancionTitulo { get; set; } = string.Empty;
        public string ArchivoAudioUrl { get; set; } = string.Empty;
        public string? PortadaUrl { get; set; }
        public TimeSpan? Duracion { get; set; }

        // Información del artista
        public int ArtistaId { get; set; }
        public string ArtistaNombre { get; set; } = string.Empty;

        // Información adicional
        public string? AlbumNombre { get; set; }
        public string GeneroNombre { get; set; } = string.Empty;
        public DateTime FechaAgregada { get; set; }
    }
}