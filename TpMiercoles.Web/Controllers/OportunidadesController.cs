using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    [Authorize]
    public class OportunidadesController : Controller
    {
        private const string ERROR_DE_RUTA = "No pudimos calcular la ruta en este momento. Por favor, intente nuevamente más tarde.";

        private readonly IOportunidadServicio _oportunidadServicio;
        private readonly IViajeServicio _viajeServicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;

        public OportunidadesController(IOportunidadServicio oportunidadServicio, IViajeServicio viajeServicio, ISesionWeb sesionWeb, IMapper mapper)
        {
            _oportunidadServicio = oportunidadServicio;
            _viajeServicio = viajeServicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
        }

        // GET /oportunidades
        // GET /oportunidades?viajeId=3&orden=Desvio&desvioMaximoKm=5
        [HttpGet("/oportunidades")]
        public async Task<IActionResult> Index(
            int? viajeId,
            CriterioOrdenOportunidad orden = CriterioOrdenOportunidad.Ganancia,
            double? desvioMaximoKm = null,
            CancellationToken ct = default)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            // solo viajes activos que todavia no salieron, los mas proximos primero
            DateTime ahora = DateTime.Now;
            List<ViajeListadoDTO> viajes = (await _viajeServicio.ListarAsync(usuarioId.Value, EstadoActividadViaje.ACTIVO))
                .Where(v => v.FechaSalida >= ahora)
                .OrderBy(v => v.FechaSalida)
                .ToList();

            OportunidadesViewModel modelo = new()
            {
                Viajes = viajes.Select(_mapper.Map<ViajeListadoViewModel>).ToList(),
                ViajeId = viajeId,
                Orden = orden,
                DesvioMaximoKm = desvioMaximoKm
            };
            
            // si entro desde el sidebar (todavia no se eligio un viaje, no se calcula nada)
            if (viajeId is null)
            {
                return View(modelo);
            }

            if (desvioMaximoKm is < 0)
            {
                ModelState.AddModelError(nameof(desvioMaximoKm), "El desvío máximo no puede ser negativo.");
                return View(modelo);
            }

            try
            {
                OportunidadesDelViajeDTO? resultado = await _oportunidadServicio.ListarAsync(viajeId.Value, usuarioId.Value, orden, desvioMaximoKm, ct);
                
                // NULL si el viaje no existe o no es del usuario (da 404 en ambos casos)
                if (resultado is null)
                {
                    return NotFound();
                }

                modelo.Resultado = _mapper.Map<OportunidadesDelViajeViewModel>(resultado);
            }
            catch (RutaNoEncontradaException)
            {
                modelo.Error = ERROR_DE_RUTA;
            }

            return View(modelo);
        }

        // GET /oportunidades/{viajeId}/envios/{envioId}
        [HttpGet("/oportunidades/{viajeId:int}/envios/{envioId:int}")]
        public async Task<IActionResult> Detalle(int viajeId, int envioId, CancellationToken ct)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            OportunidadDetalleDTO? detalle;
            try
            {
                detalle = await _oportunidadServicio.ObtenerDetalleAsync(viajeId, envioId, usuarioId.Value, ct);
            }
            catch (RutaNoEncontradaException)
            {
                TempData["Error"] = "Ruta no encontrada.";
                return RedirectToAction(nameof(Index), new { viajeId });
            }

            if (detalle is null)
            {
                return NotFound();
            }

            return View(_mapper.Map<OportunidadDetalleViewModel>(detalle));
        }

        // POST /oportunidades/{viajeId}/envios/{envioId}/aceptar
        [HttpPost("/oportunidades/{viajeId:int}/envios/{envioId:int}/aceptar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aceptar(int viajeId, int envioId)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ResultadoOperacionOportunidad resultado =
                await _oportunidadServicio.AceptarAsync(viajeId, envioId, usuarioId.Value);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            if (!resultado.EsExitoso)
            {
                // volvemos al detalle: se recalcula y muestra por que ya no se puede aceptar (por ejemplo, porque otro conductor ya lo acepto)
                TempData["Error"] = resultado.Conflicto;
                return RedirectToAction(nameof(Detalle), new { viajeId, envioId });
            }

            TempData["Mensaje"] = $"Aceptaste el envío #{envioId}. Ya figura en los paquetes del viaje #{viajeId}.";
            return RedirectToAction(nameof(Index), new { id = viajeId });
        }
    }
}
