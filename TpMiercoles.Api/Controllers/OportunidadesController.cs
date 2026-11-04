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
    [Route("api/viajes/{viajeId:int}/oportunidades")]
    [Produces("application/json")]
    public class OportunidadesController : ControllerBase
    {
        private readonly IOportunidadServicio _oportunidadServicio;

        public OportunidadesController(IOportunidadServicio oportunidadServicio)
        {
            _oportunidadServicio = oportunidadServicio;
        }

        // GET /api/viajes/{viajeId}/oportunidades
        // GET /api/viajes/{viajeId}/oportunidades?orden=Desvio&desvioMaximoKm=5
        [HttpGet]
        [ProducesResponseType(typeof(OportunidadesDelViajeDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<OportunidadesDelViajeDTO>> Listar(
            int viajeId,
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
                OportunidadesDelViajeDTO? resultado = await _oportunidadServicio.ListarAsync(viajeId, usuarioId, orden, desvioMaximoKm, ct);

                // 404 si el viaje no existe o si es de otro conductor
                return resultado is null ? NotFound() : Ok(resultado);
            }
            catch (RutaNoEncontradaException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { error = "No se pudo calcular la ruta del viaje." });
            }
        }

        // GET /api/viajes/{viajeId}/oportunidades/{envioId}
        [HttpGet("{envioId:int}")]
        [ProducesResponseType(typeof(OportunidadDetalleDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<OportunidadDetalleDTO>> Obtener(int viajeId, int envioId, CancellationToken ct)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            try
            {
                OportunidadDetalleDTO? detalle =
                    await _oportunidadServicio.ObtenerDetalleAsync(viajeId, envioId, usuarioId, ct);

                return detalle is null ? NotFound() : Ok(detalle);
            }
            catch (RutaNoEncontradaException)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { error = "No se pudo calcular la ruta del viaje." });
            }
        }

        // POST /api/viajes/{viajeId}/oportunidades/{envioId}/aceptar
        [HttpPost("{envioId:int}/aceptar")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Aceptar(int viajeId, int envioId)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            ResultadoOperacionOportunidad resultado =
                await _oportunidadServicio.AceptarAsync(viajeId, envioId, usuarioId);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            // 409: el pedido esta bien, pero el envio o el viaje ya no cumplen las restricciones.
            if (!resultado.EsExitoso)
            {
                return Conflict(new { error = resultado.Conflicto });
            }

            // La asignacion creada es un transporte: se consulta en /api/transportes/{id}.
            return Created($"/api/transportes/{resultado.AsignacionId}",
                new { asignacionId = resultado.AsignacionId, viajeId, envioId });
        }

        private bool TryObtenerUsuarioId(out int usuarioId)
        {
            string? valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(valor, out usuarioId);
        }
    }
}
