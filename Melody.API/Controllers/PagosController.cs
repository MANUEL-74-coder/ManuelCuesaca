using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Melody.API.Services;
using Melody.Modelos.PayPal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Melody.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPayPalService _payPalService;
        private readonly UserManager<Usuario> _userManager;
        private readonly IUsuarioService _usuarioService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<PagosController> _logger;

        public PagosController(AppDbContext context, IPayPalService payPalService, UserManager<Usuario> userManager,
                               IUsuarioService usuarioService, IPdfService pdfService, ILogger<PagosController> logger)
        {
            _context = context;
            _payPalService = payPalService;
            _userManager = userManager;
            _usuarioService = usuarioService;
            _pdfService = pdfService;
            _logger = logger;
        }

        // GET: api/Pagos
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<Pago>>> GetPagos()
        {
            try
            {
                var pagos = await _context.Pagos
                    .Include(p => p.Suscripcion)
                    .OrderByDescending(p => p.FechaPago)
                    .ToListAsync();

                return Ok(pagos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pagos");
                return StatusCode(500, "Error al obtener los pagos");
            }
        }

        // GET: api/Pagos/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Pago>> GetPago(int id)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                var pago = await _context.Pagos
                    .Include(p => p.Suscripcion)
                        .ThenInclude(s => s.Usuario)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pago == null)
                {
                    return NotFound("Pago no encontrado");
                }

                // Verificar permisos: admin o dueño del pago
                var roles = await _userManager.GetRolesAsync(usuario);
                if (!roles.Contains("admin") && pago.Suscripcion.UsuarioId != usuario.Id)
                {
                    return Forbid("No tienes permisos para ver este pago");
                }

                return Ok(pago);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pago {Id}", id);
                return StatusCode(500, "Error al obtener el pago");
            }
        }
        // CAMBIAR SOLO el método CrearOrdenPayPal en tu API Controller

        [HttpPost("crear-orden")]
        [Authorize(Roles = "userfree,userpremium")]
        public async Task<ActionResult> CrearOrdenPayPal([FromBody] CrearOrdenRequest request)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                // Verificar que el plan existe
                var plan = await _context.Planes.FindAsync(request.PlanId);
                if (plan == null)
                {
                    return NotFound("Plan no encontrado");
                }

                // Verificar si ya tiene una suscripción activa
                var suscripcionActiva = await _context.Suscripciones
                    .FirstOrDefaultAsync(s => s.UsuarioId == usuario.Id && s.EsActiva);

                if (suscripcionActiva != null)
                {
                    return BadRequest("Ya tienes una suscripción activa");
                }

                // Crear orden en PayPal y obtener URL de aprobación
                var (orderId, approvalUrl) = await _payPalService.CrearOrdenAsync(plan.Precio);

                _logger.LogInformation("Orden PayPal creada {OrderId} para usuario {Email} y plan {PlanNombre}",
                    orderId, usuario.Email, plan.Nombre);

                return Ok(new
                {
                    OrderId = orderId,
                    ApprovalUrl = approvalUrl,
                    Monto = plan.Precio,
                    Plan = new
                    {
                        plan.Id,
                        plan.Nombre,
                        plan.Descripcion,
                        plan.Precio
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear orden PayPal");
                return StatusCode(500, "Error al crear la orden de pago");
            }
        }

        // POST: api/Pagos/capturar - Capturar pago PayPal
        [HttpPost("capturar")]
        [Authorize(Roles = "userfree,userpremium")]
        public async Task<ActionResult> CapturarPago([FromBody] CapturarPagoRequest request)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                // Capturar el pago en PayPal
                var exito = await _payPalService.CapturarPagoAsync(request.OrderId);

                if (!exito)
                {
                    return BadRequest("Error al procesar el pago en PayPal");
                }

                // Obtener detalles de la orden
                var detallesOrden = await _payPalService.ObtenerDetallesOrdenAsync(request.OrderId);

                // Verificar que el plan existe
                var plan = await _context.Planes.FindAsync(request.PlanId);
                if (plan == null)
                {
                    return BadRequest("Plan no encontrado");
                }

                // Crear suscripción
                var suscripcion = new Suscripcion
                {
                    UsuarioId = usuario.Id,
                    PlanId = request.PlanId,
                    FechaInicio = DateTime.Now,
                    FechaFin = DateTime.Now.AddDays(plan.DuracionDias),
                    EsActiva = true
                };

                _context.Suscripciones.Add(suscripcion);
                await _context.SaveChangesAsync();

                // Crear registro de pago
                var pago = new Pago
                {
                    Monto = plan.Precio,
                    FechaPago = DateTime.Now,
                    MetodoPago = "PayPal",
                    SuscripcionId = suscripcion.Id
                };

                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                // Cambiar rol del usuario a premium
                var rolesActuales = await _userManager.GetRolesAsync(usuario);
                if (rolesActuales.Contains("userfree"))
                {
                    await _userManager.RemoveFromRoleAsync(usuario, "userfree");
                    await _userManager.AddToRoleAsync(usuario, "userpremium");
                }

                _logger.LogInformation("Pago procesado exitosamente. Usuario {Email} ahora tiene suscripción premium",
                    usuario.Email);

                return Ok(new
                {
                    Mensaje = "Pago procesado exitosamente",
                    Suscripcion = new
                    {
                        suscripcion.Id,
                        suscripcion.FechaInicio,
                        suscripcion.FechaFin,
                        Plan = new
                        {
                            plan.Id,
                            plan.Nombre,
                            plan.Precio
                        }
                    },
                    Pago = new
                    {
                        pago.Id,
                        pago.Monto,
                        pago.FechaPago,
                        pago.MetodoPago
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al capturar pago PayPal");
                return StatusCode(500, "Error al procesar el pago");
            }
        }

        // GET: api/Pagos/mis-pagos - Obtener mis pagos
        [HttpGet("mis-pagos")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerMisPagos()
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                var pagos = await _context.Pagos
                    .Include(p => p.Suscripcion)
                        .ThenInclude(s => s.Plan)
                    .Where(p => p.Suscripcion.UsuarioId == usuario.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Monto,
                        p.FechaPago,
                        p.MetodoPago,
                        Plan = new
                        {
                            p.Suscripcion.Plan.Id,
                            p.Suscripcion.Plan.Nombre,
                            p.Suscripcion.Plan.Precio
                        }
                    })
                    .OrderByDescending(p => p.FechaPago)
                    .ToListAsync();

                return Ok(pagos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener pagos del usuario");
                return StatusCode(500, "Error al obtener los pagos");
            }

        }// GET: api/Pagos/{id}/pdf - Descargar comprobante PDF
        [HttpGet("{id}/pdf")]
        [Authorize]
        public async Task<IActionResult> DescargarComprobantePdf(int id)
        {
            try
            {
                var usuario = await _usuarioService.ObtenerUsuarioActualAsync();

                // Verificamos que el pago exista
                var pago = await _context.Pagos
                    .Include(p => p.Suscripcion)
                        .ThenInclude(s => s.Usuario)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (pago == null)
                {
                    return NotFound("Pago no encontrado");
                }

                // Verificar permisos: admin o dueño del pago
                var roles = await _userManager.GetRolesAsync(usuario);
                bool esAdmin = roles.Contains("admin");
                bool esPropietario = pago.Suscripcion.UsuarioId == usuario.Id;
                if (!esAdmin && !esPropietario)
                {
                    return Forbid("No tienes permisos para descargar este comprobante");
                }

                var pdfBytes = await _pdfService.GenerarComprobantePagoAsync(id);
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogError("PDF generado está vacío para pago {PagoId}", id);
                    return StatusCode(500, "Error: PDF generado está vacío");
                }

                var fileName = $"comprobante-pago-{id}.pdf";
                _logger.LogInformation("PDF generado exitosamente para pago {PagoId}. Tamaño: {Size} bytes",
                    id, pdfBytes.Length);

                // Asegurar headers correctos para el PDF
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
                Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                Response.Headers.Add("Pragma", "no-cache");
                Response.Headers.Add("Expires", "0");

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar PDF para pago {PagoId}", id);
                return StatusCode(500, new
                {
                    mensaje = "Error al generar PDF",
                    detalle = ex.Message
                });
            }
        }
    }
}