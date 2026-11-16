using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.Viajes;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Ganancias;
using TpMiercoles.Servicios.Viajes;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEnvioServicio _envioServicio;
        private readonly IViajeServicio _viajeServicio;
        private readonly IOportunidadServicio _oportunidadServicio;
        private readonly IGananciasServicio _gananciasServicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;
        private readonly TimeProvider _reloj;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IEnvioServicio envioServicio,
            IViajeServicio viajeServicio,
            IOportunidadServicio oportunidadServicio,
            IGananciasServicio gananciasServicio,
            ISesionWeb sesionWeb,
            IMapper mapper,
            TimeProvider reloj,
            ILogger<HomeController> logger)
        {
            _envioServicio = envioServicio;
            _viajeServicio = viajeServicio;
            _oportunidadServicio = oportunidadServicio;
            _gananciasServicio = gananciasServicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
            _reloj = reloj;
            _logger = logger;
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

            List<ViajeListadoDTO> viajesActivos =
                await _viajeServicio.ListarAsync(usuarioId.Value, EstadoActividadViaje.ACTIVO);

            var proximosViajes = viajesActivos
                .Where(v => v.FechaSalida >= DateTime.Now)
                .OrderBy(v => v.FechaSalida)
                .ToList();

            int oportunidades = await _oportunidadServicio.ContarDisponiblesAsync(usuarioId.Value);

            decimal? potencialMes = await CalcularPotencialDelMesAsync(usuarioId.Value, ct);

            var modelo = new DashboardViewModel
            {
                NombreUsuario = User.Identity?.Name ?? string.Empty,
                EnviosPendientes = pendientes.Count,
                EnviosPendientesLista = pendientes,
                ProximosViajes = proximosViajes.Count,
                ProximosViajesLista = proximosViajes.Take(5).Select(_mapper.Map<ViajeListadoViewModel>).ToList(),
                Oportunidades = oportunidades,
                PotencialMes = potencialMes
            };

            return View(modelo);
        }

        // Mismo calculo que la pantalla de Potencial de ganancias, con los valores por defecto
        // (mes actual, Mayor ganancia, sin limite de desvio). Si falla (por ejemplo, el servicio
        // de rutas no responde) el inicio igual carga y la tarjeta muestra un guion.
        private async Task<decimal?> CalcularPotencialDelMesAsync(int usuarioId, CancellationToken ct)
        {
            DateTimeOffset hoy = _reloj.GetLocalNow();

            try
            {
                GananciasMensualDTO ganancias = await _gananciasServicio.CalcularAsync(
                    usuarioId, hoy.Year, hoy.Month, CriterioOptimizacion.MayorGanancia, null, ct);

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
    }
}
