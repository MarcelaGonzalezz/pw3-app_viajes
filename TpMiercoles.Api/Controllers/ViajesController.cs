using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Api.Controllers
{
    // Web API REST de viajes. Devuelve DTOs, nunca entidades.
    // Comparte la capa de servicio con el controlador MVC para no duplicar
    // logica de negocio entre las dos interfaces.
    [ApiController]
    [Authorize]
    [Route("api/viajes")]
    [Produces("application/json")]
    public class ViajesController : ControllerBase
    {
        private readonly IViajeServicio _viajeServicio;

        public ViajesController(IViajeServicio viajeServicio)
        {
            _viajeServicio = viajeServicio;
        }

        // GET /api/viajes
        // GET /api/viajes?estado=ACTIVO
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ViajeListadoDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<ViajeListadoDTO>>> Listar([FromQuery] EstadoActividadViaje? estado)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            List<ViajeListadoDTO> viajes = await _viajeServicio.ListarAsync(usuarioId, estado);
            return Ok(viajes);
        }

        // GET /api/viajes/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ViajeDetalleDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ViajeDetalleDTO>> Obtener(int id)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            ViajeDetalleDTO? viaje = await _viajeServicio.ObtenerDetalleAsync(id, usuarioId);

            // 404 tanto si no existe como si es de otro usuario: no delatamos que existe.
            return viaje is null ? NotFound() : Ok(viaje);
        }

        // PUT /api/viajes/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ViajeEdicionDTO datos)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            ResultadoOperacionViaje resultado = await _viajeServicio.ActualizarAsync(id, usuarioId, datos);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            if (!resultado.EsExitoso)
            {
                return BadRequest(new { errores = resultado.Errores });
            }

            return NoContent();
        }

        // POST /api/viajes/{id}/desactivar  (baja logica)
        [HttpPost("{id:int}/desactivar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Desactivar(int id)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            ResultadoOperacionViaje resultado = await _viajeServicio.DesactivarAsync(id, usuarioId);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            return Ok(new
            {
                id,
                estado = EstadoActividadViaje.INACTIVO.ToString(),
                paquetesAsignados = resultado.PaquetesAfectados
            });
        }

        // POST /api/viajes/{id}/activar
        [HttpPost("{id:int}/activar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Activar(int id)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            ResultadoOperacionViaje resultado = await _viajeServicio.ActivarAsync(id, usuarioId);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            return Ok(new
            {
                id,
                estado = EstadoActividadViaje.ACTIVO.ToString(),
                paquetesAsignados = resultado.PaquetesAfectados
            });
        }

        private bool TryObtenerUsuarioId(out int usuarioId)
        {
            string? valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(valor, out usuarioId);
        }
    }
}
