using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class AgregarCancionPlaylistDto
    {
        [Required(ErrorMessage = "El ID de la playlist es requerido")]
        public int PlaylistId { get; set; }

        [Required(ErrorMessage = "El ID de la canción es requerido")]
        public int CancionId { get; set; }
    }
}