using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    [Authorize]
    public sealed class OptimizadorController : Controller
    {
        private readonly IOptimizadorServicio _optimizador;
        private readonly IOportunidadServicio _oportunidadServicio;
        private readonly ISesionWeb _sesionWeb;

        public OptimizadorController(IOptimizadorServicio optimizador, ISesionWeb sesionWeb, IOportunidadServicio oportunidadServicio)
        {
            _optimizador = optimizador;
            _sesionWeb = sesionWeb;
            _oportunidadServicio = oportunidadServicio;
        }

        // salida y desvio son opcionales: los usan "Volver" y "Recalcular" de la confirmación
        // para dejar cargada la misma búsqueda.
        [HttpGet("/optimizador")]
        public async Task<IActionResult> Index(
            CancellationToken ct,
            string? salida = null,
            double? desvio = null) {
            int? conductorId = _sesionWeb.UsuarioId;

            if (conductorId is null) {
                return Challenge();
            }

            var salidas = await _optimizador.ListarSalidasAsync(
                conductorId.Value,
                ct);

            var modelo = new OptimizadorViewModel {
                Salidas = salidas,
                SalidaSeleccionada = salida,
                DesvioMaximoKm = desvio
            };

            return View(modelo);
        }

        [HttpPost("/optimizador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
    OptimizadorViewModel modelo,
    CancellationToken ct)
        {
            int? conductorId = _sesionWeb.UsuarioId;

            if (conductorId is null)
            {
                return Challenge();
            }

            // Estas propiedades siempre se completan desde el servidor.
            modelo.Resultado = null;

            modelo.Salidas = await _optimizador.ListarSalidasAsync(
                conductorId.Value,
                ct);

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string[] partes = modelo.SalidaSeleccionada!.Split(':');

            if (partes.Length != 2
                || !int.TryParse(partes[0], out int rutaId)
                || rutaId <= 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.SalidaSeleccionada),
                    "La salida seleccionada no es válida.");

                return View(modelo);
            }

            int? viajeId = null;

            if (!string.IsNullOrEmpty(partes[1]))
            {
                if (!int.TryParse(partes[1], out int id)
                    || id <= 0)
                {
                    ModelState.AddModelError(
                        nameof(modelo.SalidaSeleccionada),
                        "El viaje seleccionado no es válido.");

                    return View(modelo);
                }

                viajeId = id;
            }

            double desvioMaximoKm = modelo.DesvioMaximoKm!.Value;

            if (!double.IsFinite(desvioMaximoKm)
                || desvioMaximoKm < 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.DesvioMaximoKm),
                    "El desvío máximo debe ser un número finito "
                    + "mayor o igual a cero.");

                return View(modelo);
            }

            var solicitud = new SolicitudOptimizacionDTO
            {
                RutaId = rutaId,
                ViajeId = viajeId,
                DesvioMaximoKm = desvioMaximoKm
            };

            try
            {
                ResultadoOptimizacionDTO resultado =
                    await _optimizador.CalcularAsync(
                        solicitud,
                        conductorId.Value,
                        ct);

                if (resultado.Estado != EstadoResultadoOptimizacion.Correcto)
                {
                    foreach (string mensaje in resultado.Mensajes)
                    {
                        ModelState.AddModelError(string.Empty, mensaje);
                    }

                    return View(modelo);
                }

                modelo.Resultado = resultado;
            }
            catch (RutaNoEncontradaException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No pudimos calcular el recorrido en este momento. "
                    + "Intentá nuevamente más tarde.");
            }

            return View(modelo);
        }
        // Pantalla 11: resumen de la propuesta antes de reservar. No modifica nada.
        [HttpPost("/optimizador/confirmar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmar(ConfirmarPropuestaViewModel modelo) {
            int? conductorId = _sesionWeb.UsuarioId;

            if (conductorId is null) {
                return Challenge();
            }

            if (!ModelState.IsValid) {
                TempData["Error"] =
                    "Los datos de la propuesta no son válidos. Volvé a calcularla.";

                return VolverAlOptimizador(modelo.SalidaSeleccionada, modelo.DesvioMaximoKm);
            }

            ResumenPropuestaDTO? resumen =
                await _oportunidadServicio.ObtenerResumenPropuestaAsync(
                    modelo.RutaId, modelo.ViajeId, modelo.FechaSalidaProgramada!.Value, modelo.EnvioIds, conductorId.Value);

            if (resumen is null) {
                TempData["Error"] =
                    "La salida de la propuesta ya no está disponible. Volvé a calcularla.";

                return VolverAlOptimizador(modelo.SalidaSeleccionada, modelo.DesvioMaximoKm);
            }

            modelo.Resumen = resumen;
            return View(modelo);
        }

        [HttpPost("/optimizador/aceptar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AceptarPropuesta(AceptarPropuestaViewModel modelo) {
            int? conductorId = _sesionWeb.UsuarioId;

            if (conductorId is null) {
                return Challenge();
            }

            if (!ModelState.IsValid) {
                TempData["Error"] =
                    "Los datos de la propuesta no son válidos. Volvé a calcularla.";

                return VolverAlOptimizador(modelo.SalidaSeleccionada, modelo.DesvioMaximoKm);
            }

            ResultadoAceptacionPropuestaDTO resultado =
                await _oportunidadServicio.AceptarPropuestaAsync(
                    modelo.RutaId, modelo.ViajeId, modelo.FechaSalidaProgramada!.Value, modelo.EnvioIds, conductorId.Value);

            if (resultado.EsExitoso) {
                TempData["Mensaje"] =
                    $"Se aceptaron {resultado.EnviosAceptadosIds.Count} envíos "
                    + $"para el viaje #{resultado.ViajeId}.";

                return RedirectToAction("Detalle", "Viajes", new { id = resultado.ViajeId });
            }

            // Todo o nada: si falló, no se aceptó ningún envío. Se vuelve al optimizador con la misma
            // búsqueda cargada, para recalcular o cancelar.
            TempData["Error"] =
                (resultado.Error ?? "No se pudo aceptar la propuesta.")
                + " Podés recalcular las propuestas con los envíos que siguen disponibles.";

            return VolverAlOptimizador(modelo.SalidaSeleccionada, modelo.DesvioMaximoKm);
        }

        private RedirectToActionResult VolverAlOptimizador(string? salida, double? desvio) =>
            RedirectToAction(nameof(Index), new { salida, desvio });
    }


}
