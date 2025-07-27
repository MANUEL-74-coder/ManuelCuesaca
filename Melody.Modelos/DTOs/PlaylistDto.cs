using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class PlaylistDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Imagen { get; set; }
        public bool EsPublica { get; set; }
        public int TotalCanciones { get; set; }
        public double DuracionTotal { get; set; } // En minutos

        // Información del creador
        public int CreadorId { get; set; }
        public string CreadorNombre { get; set; } = string.Empty;
        public string CreadorApellido { get; set; } = string.Empty;
        public string? CreadorFotoPerfil { get; set; }

        // Agregamos fecha de creación
        public DateTime FechaCreacion { get; set; }

        // Lista de canciones (para la vista de detalles)
        public List<CancionPlaylistDto>? Canciones { get; set; }
    }
}
