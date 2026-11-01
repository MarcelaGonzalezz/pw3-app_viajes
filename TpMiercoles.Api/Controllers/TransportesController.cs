using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/transportes")]
    [Produces("application/json")]
    public class TransportesController : ControllerBase
    {
        private readonly ITransporteServicio _transporteServicio;

        public TransportesController(ITransporteServicio transporteServicio)
        {
            _transporteServicio = transporteServicio;
        }

        // GET /api/transportes
        // GET /api/transportes?estado=Retirado
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TransporteListadoDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<TransporteListadoDTO>>> Listar([FromQuery] EstadoEnvio? estado)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            List<TransporteListadoDTO> transportes = await _transporteServicio.ListarAsync(usuarioId, estado);
            return Ok(transportes);
        }

        // GET /api/transportes/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(TransporteDetalleDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TransporteDetalleDTO>> Obtener(int id)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            TransporteDetalleDTO? transporte = await _transporteServicio.ObtenerDetalleAsync(id, usuarioId);

            // 404 tanto si no existe como si el viaje es de otro conductor.
            return transporte is null ? NotFound() : Ok(transporte);
        }

        // POST /api/transportes/{id}/retirar
        [HttpPost("{id:int}/retirar")]
        [ProducesResponseType(typeof(TransporteListadoDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> Retirar(int id) => Avanzar(id, EstadoEnvio.Retirado);

        // POST /api/transportes/{id}/en-camino
        [HttpPost("{id:int}/en-camino")]
        [ProducesResponseType(typeof(TransporteListadoDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> MarcarEnCamino(int id) => Avanzar(id, EstadoEnvio.EnCamino);

        // POST /api/transportes/{id}/entregar
        [HttpPost("{id:int}/entregar")]
        [ProducesResponseType(typeof(TransporteListadoDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> Entregar(int id) => Avanzar(id, EstadoEnvio.Entregado);

        private async Task<IActionResult> Avanzar(int id, EstadoEnvio destino)
        {
            if (!TryObtenerUsuarioId(out int usuarioId))
            {
                return Unauthorized();
            }

            ResultadoOperacionTransporte resultado =
                await _transporteServicio.AvanzarAsync(id, usuarioId, destino);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            // 409 y no 400: el pedido esta bien formado, lo que no corresponde es
            // el estado en el que esta el paquete ahora mismo.
            if (!resultado.EsExitoso)
            {
                return Conflict(new { error = resultado.Conflicto });
            }

            return Ok(resultado.Transporte);
        }

        private bool TryObtenerUsuarioId(out int usuarioId)
        {
            string? valor = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(valor, out usuarioId);
        }
    }
}
