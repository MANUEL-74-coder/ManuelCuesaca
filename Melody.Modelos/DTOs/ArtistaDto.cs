using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{

    public class ArtistaDto
    {
        public int Id { get; set; }
        public string NombreArtista { get; set; } = string.Empty;
        public string? Biografia { get; set; }
        public string? ImagenPerfil { get; set; }
        public int TotalCanciones { get; set; }
        public int TotalAlbums { get; set; }
        public int TotalSeguidores { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool EstaSiguiendo { get; set; } = false;
        public bool EsMiPerfil { get; set; } = false;
        public List<CancionDto> Canciones { get; set; } = new List<CancionDto>();
        public string? NombreUsuario { get; set; }
        public string? ApellidoUsuario { get; set; }
        public string? EmailUsuario { get; set; }
    }

}