using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Melody.Modelos.DTOs
{
    public class CrearAlbumDto
    {
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(100, ErrorMessage = "El título no puede exceder los 100 caracteres")]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "El género es requerido")]
        public int GeneroId { get; set; }

        public IFormFile? Portada { get; set; }

    }
}