using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.Excepciones;

namespace TpMiercoles.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/rutas/{rutaId:int}/oportunidades")]
    [Produces("application/json")]
    public class OportunidadesController : ControllerBase
    {
        private readonly IOportunidadServicio _oportunidadServicio;

        public OportunidadesController(IOportunidadServicio oportunidadServicio)
        {
            _oportunidadServicio = oportunidadServicio;
        }

        // GET /api/rutas/{rutaId}/oportunidades
        // GET /api/rutas/{rutaId}/oportunidades?fecha=2026-11-09&orden=Desvio&desvioMaximoKm=5
        /// <summary>
        /// Busca los envíos compatibles con una salida de la ruta del conductor.
        /// </summary>
        /// <remarks>
        /// Solo considera envíos disponibles (sin conductor y no cancelados) cuya fecha incluya el día de la salida,
        /// que entren en la capacidad y el peso que le quedan al viaje, y que no sean del propio conductor.
        /// Para cada uno calcula el desvío, el tiempo adicional y la ganancia por km adicional.
        /// </remarks>
        /// <param name="rutaId">Id de la ruta del conductor.</param>
        /// <param name="fecha">Salida a evaluar. Si no se envía, se usa la próxima salida de la ruta.</param>
        /// <param name="orden">Criterio de orden: Ganancia (por defecto), Desvio o Compatibilidad.</param>
        /// <param name="desvioMaximoKm">Opcional. Por defecto 10 km.</param>
        /// <response code="200">Salida evaluada y lista de oportunidades (puede estar vacía).</response>
        /// <response code="400">El desvío máximo es negativo.</response>
        /// <response code="404">La ruta no existe, es de otro usuario o no tiene una salida válida en esa fecha.</response>
        /// <response code="401">No hay sesión iniciada.</response>
        /// <response code="502">El servicio de rutas (OpenRouteService) no respondió.</response>
        [HttpGet]
        [ProducesResponseType(typeof(OportunidadesDeRutaDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<OportunidadesDeRutaDTO>> Listar(
            int rutaId,
            [FromQuery] DateTime? fecha = null,
            [FromQuery] CriterioOrdenOportunidad orden = CriterioOrdenOportunidad.Ganancia,
            [FromQuery] double? desvioMaximoKm = null,
            CancellationToken ct = default)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            if (desvioMaximoKm is < 0)
            {
                return BadRequest(new { error = "El desvío máximo no puede ser negativo." });
            }

            try
            {
                OportunidadesDeRutaDTO? resultado =
                    await _oportunidadServicio.ListarAsync(rutaId, usuarioId, fecha, orden, desvioMaximoKm, ct);

                // 404 si la ruta no existe, no es del conductor o no tiene una salida valida
                return resultado is null ? NotFound() : Ok(resultado);
            }
            catch (RutaNoEncontradaException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { error = "No se pudo calcular la ruta de la salida." });
            }
        }

        // GET /api/rutas/{rutaId}/oportunidades/{envioId}?fecha=2026-11-09
        /// <summary>
        /// Explica por qué un envío es compatible con una salida de la ruta.
        /// </summary>
        /// <remarks>
        /// Compara el recorrido habitual con el recorrido pasando por el retiro y la entrega:
        /// distancias, desvío, tiempo adicional, pago, peso, capacidad restante y ganancia por km adicional.
        /// Si el envío ya no se puede aceptar, lo indica con EsAceptable = false y explica por qué en Motivos.
        /// </remarks>
        /// <param name="rutaId">Id de la ruta del conductor.</param>
        /// <param name="envioId">Id del envío.</param>
        /// <param name="fecha">Salida a evaluar. Si no se envía, se usa la próxima salida de la ruta.</param>
        /// <response code="200">Detalle de la oportunidad.</response>
        /// <response code="404">La ruta o el envío no existen, o la ruta es de otro usuario.</response>
        /// <response code="401">No hay sesión iniciada.</response>
        /// <response code="502">El servicio de rutas (OpenRouteService) no respondió.</response>
        [HttpGet("{envioId:int}")]
        [ProducesResponseType(typeof(OportunidadDetalleDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<OportunidadDetalleDTO>> Obtener(
            int rutaId,
            int envioId,
            [FromQuery] DateTime? fecha = null,
            CancellationToken ct = default)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            try
            {
                OportunidadDetalleDTO? detalle =
                    await _oportunidadServicio.ObtenerDetalleAsync(rutaId, envioId, usuarioId, fecha, ct);

                return detalle is null ? NotFound() : Ok(detalle);
            }
            catch (RutaNoEncontradaException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { error = "No se pudo calcular la ruta de la salida." });
            }
        }

        // POST /api/rutas/{rutaId}/oportunidades/{envioId}/aceptar?fecha=2026-11-09
        /// <summary>
        /// Acepta una oportunidad: asigna el envío al viaje de esa salida.
        /// </summary>
        /// <remarks>
        /// Antes de aceptar vuelve a controlar que el envío siga disponible y que entre en el viaje.
        /// Si el viaje de esa fecha todavía no existía, lo crea. El envío pasa a "Conductor asignado".
        /// </remarks>
        /// <param name="rutaId">Id de la ruta del conductor.</param>
        /// <param name="envioId">Id del envío a aceptar.</param>
        /// <param name="fecha">Salida en la que se va a llevar. Si no se envía, se usa la próxima salida de la ruta.</param>
        /// <response code="201">Envío aceptado. Devuelve el id del transporte creado y el del viaje.</response>
        /// <response code="404">La ruta o el envío no existen, o la ruta es de otro usuario.</response>
        /// <response code="409">El envío ya no está disponible o ya no entra en el viaje (por ejemplo, lo tomó otro conductor).</response>
        /// <response code="401">No hay sesión iniciada.</response>
        [HttpPost("{envioId:int}/aceptar")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Aceptar(
            int rutaId,
            int envioId,
            [FromQuery] DateTime? fecha = null)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            ResultadoOperacionOportunidad resultado =
                await _oportunidadServicio.AceptarAsync(rutaId, envioId, usuarioId, fecha);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            // 409: el pedido esta bien, pero el envio o la salida ya no cumplen las restricciones.
            if (!resultado.EsExitoso)
            {
                return Conflict(new { error = resultado.Conflicto });
            }

            // La asignacion creada es un transporte: se consulta en /api/transportes/{id}.
            // viajeId puede ser un viaje recien materializado por esta aceptacion.
            return Created($"/api/transportes/{resultado.AsignacionId}",
                new { asignacionId = resultado.AsignacionId, viajeId = resultado.ViajeId, envioId });
        }

        private bool TryObtenerUsuarioId(out int usuarioId)
        {
            string? valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(valor, out usuarioId);
        }
    }
}
