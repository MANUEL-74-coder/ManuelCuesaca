using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melody.Modelos.PayPal
{
    public class CapturarPagoRequest
    {
        public string OrderId { get; set; }
        public int PlanId { get; set; }
    }
}