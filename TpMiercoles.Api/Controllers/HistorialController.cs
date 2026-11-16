using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TpMiercoles.Entidades;
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
        /// <summary>
        /// Consulta las operaciones anteriores del usuario logueado.
        /// </summary>
        /// <remarks>
        /// Incluye los viajes realizados como conductor (con su ganancia) y los envíos propios entregados o cancelados.
        /// Devuelve un resumen (viajes realizados, envíos transportados y ganancia efectiva) y el detalle de cada operación.
        /// Nunca muestra información de otros usuarios.
        /// </remarks>
        /// <param name="tipo">Filtro opcional: ViajeRealizado, EnvioEntregado o EnvioCancelado.</param>
        /// <param name="desde">Filtro opcional: fecha desde (inclusive).</param>
        /// <param name="hasta">Filtro opcional: fecha hasta (inclusive).</param>
        /// <response code="200">Resumen y lista de operaciones (puede estar vacía).</response>
        /// <response code="400">La fecha "desde" es posterior a la fecha "hasta".</response>
        /// <response code="401">No hay sesión iniciada.</response>
        [HttpGet]
        [ProducesResponseType(typeof(HistorialDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<HistorialDTO>> Consultar(
            [FromQuery] TipoOperacionHistorial? tipo,
            [FromQuery] EstadoActividadViaje? estado,
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

            HistorialDTO historial = await _historialServicio.ObtenerAsync(usuarioId, tipo, estado, desde, hasta);
            return Ok(historial);
        }
    }
}
