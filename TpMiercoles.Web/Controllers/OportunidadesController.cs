using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
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
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;

        public OportunidadesController(IOportunidadServicio oportunidadServicio, ISesionWeb sesionWeb, IMapper mapper)
        {
            _oportunidadServicio = oportunidadServicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
        }

        // GET /oportunidades
        // GET /oportunidades?rutaId=3&fecha=2026-11-09&orden=Desvio&desvioMaximoKm=5
        [HttpGet("/oportunidades")]
        public async Task<IActionResult> Index(
            int? rutaId,
            DateTime? fecha,
            CriterioOrdenOportunidad orden = CriterioOrdenOportunidad.Ganancia,
            double? desvioMaximoKm = null,
            CancellationToken ct = default)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            List<SalidaDeRutaDTO> salidas =
                await _oportunidadServicio.ListarSalidasAsync(usuarioId.Value, ct);

            OportunidadesViewModel modelo = new()
            {
                Rutas = salidas.Select(_mapper.Map<SalidaDeRutaViewModel>).ToList(),
                RutaId = rutaId,
                Fecha = fecha,
                Orden = orden,
                DesvioMaximoKm = desvioMaximoKm
            };

            // si entro desde el sidebar (todavia no se eligio una ruta, no se calcula nada)
            if (rutaId is null)
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
                OportunidadesDeRutaDTO? resultado =
                    await _oportunidadServicio.ListarAsync(rutaId.Value, usuarioId.Value, fecha, orden, desvioMaximoKm, ct);

                // NULL si la ruta no existe, no es del usuario o no tiene una salida valida
                if (resultado is null)
                {
                    return NotFound();
                }

                modelo.Resultado = _mapper.Map<OportunidadesDeRutaViewModel>(resultado);
            }
            catch (RutaNoEncontradaException)
            {
                modelo.Error = ERROR_DE_RUTA;
            }

            return View(modelo);
        }

        // GET /oportunidades/{rutaId}/envios/{envioId}?fecha=2026-11-09
        [HttpGet("/oportunidades/{rutaId:int}/envios/{envioId:int}")]
        public async Task<IActionResult> Detalle(int rutaId, int envioId, DateTime? fecha, CancellationToken ct)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            OportunidadDetalleDTO? detalle;
            try
            {
                detalle = await _oportunidadServicio.ObtenerDetalleAsync(rutaId, envioId, usuarioId.Value, fecha, ct);
            }
            catch (RutaNoEncontradaException)
            {
                TempData["Error"] = "Ruta no encontrada.";
                return RedirectToAction(nameof(Index), new { rutaId, fecha });
            }

            if (detalle is null)
            {
                return NotFound();
            }

            return View(_mapper.Map<OportunidadDetalleViewModel>(detalle));
        }

        // POST /oportunidades/{rutaId}/envios/{envioId}/aceptar?fecha=2026-11-09
        [HttpPost("/oportunidades/{rutaId:int}/envios/{envioId:int}/aceptar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aceptar(int rutaId, int envioId, DateTime? fecha)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ResultadoOperacionOportunidad resultado =
                await _oportunidadServicio.AceptarAsync(rutaId, envioId, usuarioId.Value, fecha);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            if (!resultado.EsExitoso)
            {
                // volvemos al detalle: se recalcula y muestra por que ya no se puede aceptar (por ejemplo, porque otro conductor ya lo acepto)
                TempData["Error"] = resultado.Conflicto;
                return RedirectToAction(nameof(Detalle), new { rutaId, envioId, fecha });
            }

            TempData["Mensaje"] =
                $"Aceptaste el envío #{envioId}. Quedó asignado al viaje #{resultado.ViajeId}.";

            return RedirectToAction(nameof(Index), new { rutaId, fecha });
        }
    }
}
