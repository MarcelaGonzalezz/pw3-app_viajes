using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    // Mis transportes: los paquetes que el usuario acepto llevar como conductor.
    // Toda la logica (de quien es cada transporte, que transiciones valen) vive en
    // ITransporteServicio y es la misma que usa la Web API: aca solo se arma la pantalla.
    [Authorize]
    public class TransportesController : Controller
    {
        private readonly ITransporteServicio _servicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;

        public TransportesController(ITransporteServicio servicio, ISesionWeb sesionWeb, IMapper mapper)
        {
            _servicio = servicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
        }

        // GET /transportes
        // Todos los paquetes que el conductor acepto llevar. Un paquete entregado no
        // desaparece: queda en la lista con estado "Entregado" y sin accion siguiente.
        [HttpGet("/transportes")]
        public async Task<IActionResult> Index()
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            List<TransporteListadoDTO> transportes = await _servicio.ListarAsync(usuarioId.Value, null);

            MisTransportesViewModel modelo = new()
            {
                Transportes = transportes.Select(_mapper.Map<TransporteListadoViewModel>).ToList()
            };

            return View(modelo);
        }

        // GET /transportes/{id}  ("Ver envio": el paquete visto por el conductor)
        [HttpGet("/transportes/{id:int}")]
        public async Task<IActionResult> Detalle(int id)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            TransporteDetalleDTO? transporte = await _servicio.ObtenerDetalleAsync(id, usuarioId.Value);

            // El servicio devuelve null tanto si no existe como si es de otro conductor:
            // respondemos 404 en los dos casos para no delatar que existe.
            if (transporte is null)
            {
                return NotFound();
            }

            return View(_mapper.Map<TransporteDetalleViewModel>(transporte));
        }

        [HttpPost("/transportes/{id:int}/retirar")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Retirar(int id, bool volverADetalle = false)
            => Avanzar(id, EstadoEnvio.Retirado, volverADetalle);

        [HttpPost("/transportes/{id:int}/en-camino")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MarcarEnCamino(int id, bool volverADetalle = false)
            => Avanzar(id, EstadoEnvio.EnCamino, volverADetalle);

        [HttpPost("/transportes/{id:int}/entregar")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Entregar(int id, bool volverADetalle = false)
            => Avanzar(id, EstadoEnvio.Entregado, volverADetalle);

        private async Task<IActionResult> Avanzar(int id, EstadoEnvio destino, bool volverADetalle)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ResultadoOperacionTransporte resultado = await _servicio.AvanzarAsync(id, usuarioId.Value, destino);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            if (!resultado.EsExitoso)
            {
                // La transicion no corresponde (ej: entregar sin haber retirado).
                TempData["Error"] = resultado.Conflicto;
            }
            else
            {
                TempData["Mensaje"] = MensajeDeExito(resultado.Transporte!.Descripcion, destino);
            }

            return volverADetalle
                ? RedirectToAction(nameof(Detalle), new { id })
                : RedirectToAction(nameof(Index));
        }

        private static string MensajeDeExito(string descripcion, EstadoEnvio destino)
        {
            string paquete = string.IsNullOrWhiteSpace(descripcion) ? "El paquete" : $"\u201c{descripcion}\u201d";

            return destino switch
            {
                EstadoEnvio.Retirado => $"{paquete} quedó marcado como retirado.",
                EstadoEnvio.EnCamino => $"{paquete} quedó marcado como en camino.",
                EstadoEnvio.Entregado => $"{paquete} quedó marcado como entregado.",
                _ => $"{paquete} se actualizó."
            };
        }
    }
}
