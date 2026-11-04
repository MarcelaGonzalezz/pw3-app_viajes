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
