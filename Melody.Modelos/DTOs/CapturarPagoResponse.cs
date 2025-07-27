using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.DTOs
{
    public class CapturarPagoResponseDto
    {
        public string Mensaje { get; set; } = string.Empty;
        public int PagoId { get; set; }
    }

}
