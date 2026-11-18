using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.Viajes;

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
        /// <summary>
        /// Lista los viajes del usuario logueado.
        /// </summary>
        /// <param name="estado">Filtro opcional: ACTIVO o INACTIVO. Si no se envía, trae todos.</param>
        /// <response code="200">Lista de viajes del usuario (puede estar vacía).</response>
        /// <response code="401">No hay sesión iniciada.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ViajeListadoDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<ViajeListadoDTO>>> Listar([FromQuery] EstadoActividadViaje? estado)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            List<ViajeListadoDTO> viajes = await _viajeServicio.ListarAsync(usuarioId, estado, DateTime.Now);
            return Ok(viajes);
        }

        // GET /api/viajes/{id}
        /// <summary>
        /// Devuelve el detalle de un viaje: recorrido, paradas, días, capacidad y paquetes asignados.
        /// </summary>
        /// <param name="id">Id del viaje.</param>
        /// <response code="200">Detalle del viaje.</response>
        /// <response code="404">El viaje no existe o pertenece a otro usuario.</response>
        /// <response code="401">No hay sesión iniciada.</response>
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

            ViajeDetalleDTO? viaje = await _viajeServicio.ObtenerDetalleAsync(id, usuarioId, DateTime.Now);

            // 404 tanto si no existe como si es de otro usuario: no delatamos que existe.
            return viaje is null ? NotFound() : Ok(viaje);
        }

        // PUT /api/viajes/{id}
        /// <summary>
        /// Modifica los horarios, el peso máximo y la capacidad de un viaje.
        /// </summary>
        /// <remarks>
        /// La llegada tiene que ser posterior a la salida, el peso y la capacidad mayores que cero,
        /// y la capacidad no puede quedar por debajo de los paquetes ya asignados.
        /// </remarks>
        /// <param name="id">Id del viaje.</param>
        /// <param name="datos">Nuevos valores del viaje.</param>
        /// <response code="204">Cambios guardados.</response>
        /// <response code="400">Datos inválidos: devuelve la lista de errores.</response>
        /// <response code="404">El viaje no existe o pertenece a otro usuario.</response>
        /// <response code="401">No hay sesión iniciada.</response>
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
        /// <summary>
        /// Desactiva un viaje (baja lógica): deja de buscar oportunidades.
        /// </summary>
        /// <remarks>
        /// Los paquetes ya asignados se mantienen y hay que entregarlos igual.
        /// La respuesta informa cuántos paquetes tiene asignados.
        /// </remarks>
        /// <param name="id">Id del viaje.</param>
        /// <response code="200">Viaje desactivado.</response>
        /// <response code="404">El viaje no existe o pertenece a otro usuario.</response>
        /// <response code="401">No hay sesión iniciada.</response>
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
        /// <summary>
        /// Vuelve a activar un viaje para que busque oportunidades de nuevo.
        /// </summary>
        /// <param name="id">Id del viaje.</param>
        /// <response code="200">Viaje activado.</response>
        /// <response code="400">El viaje ya venció (pasó su hora de llegada) y no se puede reactivar.</response>
        /// <response code="404">El viaje no existe o pertenece a otro usuario.</response>
        /// <response code="401">No hay sesión iniciada.</response>
        [HttpPost("{id:int}/activar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Activar(int id)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            ResultadoOperacionViaje resultado = await _viajeServicio.ActivarAsync(id, usuarioId, DateTime.Now);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            if (!resultado.EsExitoso)
            {
                return BadRequest(new { errores = resultado.Errores });
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
