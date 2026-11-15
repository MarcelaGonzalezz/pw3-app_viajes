using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Mapa;
using TpMiercoles.Servicios.Viajes;
using TpMiercoles.Web.Infraestructura;

namespace TpMiercoles.Web.Controllers {
    // Datos para el mapa del detalle del viaje. Lo consume wwwroot/js/viajes/mapa-viaje.js.
    // Va aparte del detalle para que la página cargue rápido y el mapa (que consulta ORS) llegue después.
    [Authorize]
    public class MapaViajeController : Controller {
       // Este controller no devuelve una vista sino JSON, que es lo que va a pedir el JS.
       // Si el viaje no es tuyo, responde 404, igual que el detalle.
        private readonly IMapaViajeServicio _servicio;
        private readonly ISesionWeb _sesionWeb;

        public MapaViajeController(IMapaViajeServicio servicio, ISesionWeb sesionWeb) {
            _servicio = servicio;
            _sesionWeb = sesionWeb;
        }

        [HttpGet("/api/viajes/{id:int}/mapa")]
        public async Task<IActionResult> Obtener(int id, CancellationToken ct) {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null) {
                return Unauthorized();
            }

            MapaViajeDTO? mapa = await _servicio.ObtenerAsync(id, usuarioId.Value, ct);

            return mapa is null ? NotFound() : Ok(mapa);
        }
    }
}