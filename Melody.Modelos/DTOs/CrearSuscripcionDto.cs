using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class CrearSuscripcionDto
    {
        [Required(ErrorMessage = "El ID del plan es requerido")]
        public int PlanId { get; set; }
    }
}