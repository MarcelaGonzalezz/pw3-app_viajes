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
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEnvioServicio _envioServicio;
        private readonly IOportunidadServicio _oportunidadServicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;

        public HomeController(
            IEnvioServicio envioServicio,
            IOportunidadServicio oportunidadServicio,
            ISesionWeb sesionWeb,
            IMapper mapper)
        {
            _envioServicio = envioServicio;
            _oportunidadServicio = oportunidadServicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
        }

        [Authorize]
        public async Task<IActionResult> Index()
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

            var modelo = new DashboardViewModel
            {
                NombreUsuario = User.Identity?.Name ?? string.Empty,
                EnviosPendientes = pendientes.Count,
                EnviosPendientesLista = pendientes,
                ProximosViajes = salidas.Count,
                ProximosViajesLista = salidas.Take(5).Select(_mapper.Map<SalidaDeRutaViewModel>).ToList(),
                Oportunidades = oportunidades
            };

            return View(modelo);
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