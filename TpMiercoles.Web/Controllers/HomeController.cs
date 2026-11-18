using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Ganancias;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEnvioServicio _envioServicio;
        private readonly IOportunidadServicio _oportunidadServicio;
        private readonly IGananciasServicio _gananciasServicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;
        private readonly TimeProvider _reloj;
        private readonly ILogger<HomeController> _logger;
        private readonly IPreferenciasDeGanancias _preferencias;

        public HomeController(
            IEnvioServicio envioServicio,
            IOportunidadServicio oportunidadServicio,
            IGananciasServicio gananciasServicio,
            ISesionWeb sesionWeb,
            IMapper mapper,
            TimeProvider reloj,
            ILogger<HomeController> logger,
            IPreferenciasDeGanancias preferencias)
        {
            _envioServicio = envioServicio;
            _oportunidadServicio = oportunidadServicio;
            _gananciasServicio = gananciasServicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
            _reloj = reloj;
            _logger = logger;
            _preferencias = preferencias;
        }

        [Authorize]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            List<EnvioListadoDTO> todos = await _envioServicio.ListarAsync(usuarioId.Value, null);

            var pendientes = todos
                .Select(_mapper.Map<EnvioListadoViewModel>)
                .Where(e => e.Estado != EstadoEnvio.Entregado
                         && e.Estado != EstadoEnvio.Cancelado
                         && !e.EstaVencido)
                .OrderByDescending(e => e.Id)
                .ToList();

            // Recorridos activos con su proxima salida, ordenados por fecha.
            List<SalidaDeRutaDTO> salidas = await _oportunidadServicio.ListarSalidasAsync(usuarioId.Value);

            int oportunidades = await _oportunidadServicio.ContarDisponiblesAsync(usuarioId.Value);

            PreferenciaDeGanancias preferencia =
                _preferencias.Leer(usuarioId.Value) ?? PreferenciaDeGanancias.PorDefecto;
            decimal? potencialMes = await CalcularPotencialDelMesAsync(usuarioId.Value, preferencia, ct);

            var modelo = new DashboardViewModel
            {
                NombreUsuario = User.Identity?.Name ?? string.Empty,
                EnviosPendientes = pendientes.Count,
                EnviosPendientesLista = pendientes,
                ProximosViajes = salidas.Count,
                ProximosViajesLista = salidas.Take(5).Select(_mapper.Map<SalidaDeRutaViewModel>).ToList(),
                Oportunidades = oportunidades,
                PotencialMes = potencialMes,
                PotencialDetalle = preferencia.Descripcion
            };

            return View(modelo);
        }

        // Mismo calculo que la pantalla de Potencial de ganancias para el mes actual, con el criterio
        // y el limite de desvio que el usuario eligio ahi por ultima vez (o Mayor ganancia sin
        // limite si nunca eligio). Si falla (por ejemplo, el servicio de rutas no responde)
        // el inicio igual carga y la tarjeta muestra un guion.
        private async Task<decimal?> CalcularPotencialDelMesAsync(
            int usuarioId, PreferenciaDeGanancias preferencia, CancellationToken ct)
        {
            DateTimeOffset hoy = _reloj.GetLocalNow();

            try
            {
                GananciasMensualDTO ganancias = await _gananciasServicio.CalcularAsync(
                    usuarioId, hoy.Year, hoy.Month, preferencia.Criterio, preferencia.DesvioMaximoKm, ct);

                return ganancias.GananciaPotencial;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "No se pudo calcular el potencial del mes para el usuario {UsuarioId}.", usuarioId);
                return null;
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        // Página para los códigos de error sin contenido (404, 403...). La invoca
        // UseStatusCodePagesWithReExecute en Program.cs: el código de estado original se mantiene.
        [AllowAnonymous]
        [Route("/estado/{codigo:int}")]
        public IActionResult Estado(int codigo) {
            return View(codigo);
        }
    }
}