using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Historial;

namespace TpMiercoles.Api.Controllers
{
    // Historial de operaciones del usuario autenticado. Comparte HistorialServicio con el MVC.
    [ApiController]
    [Authorize]
    [Route("api/historial")]
    [Produces("application/json")]
    public class HistorialController : ControllerBase
    {
        private readonly IHistorialServicio _historialServicio;

        public HistorialController(IHistorialServicio historialServicio)
        {
            _historialServicio = historialServicio;
        }

        // GET /api/historial
        // GET /api/historial?tipo=ViajeRealizado&desde=2026-09-01&hasta=2026-09-30
        [HttpGet]
        [ProducesResponseType(typeof(HistorialDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<HistorialDTO>> Consultar(
            [FromQuery] TipoOperacionHistorial? tipo,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int usuarioId))
            {
                return Unauthorized();
            }

            if (desde is not null && hasta is not null && desde > hasta)
            {
                return BadRequest("La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");
            }

            HistorialDTO historial = await _historialServicio.ObtenerAsync(usuarioId, tipo, desde, hasta);
            return Ok(historial);
        }
    }
}
