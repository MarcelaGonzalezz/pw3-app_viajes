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
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEnvioServicio _envioServicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;

        public HomeController(IEnvioServicio envioServicio, ISesionWeb sesionWeb, IMapper mapper)
        {
            _envioServicio = envioServicio;
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
                .Where(e => e.Estado != EstadoEnvio.Entregado && e.Estado != EstadoEnvio.Cancelado)
                .OrderByDescending(e => e.Id)
                .ToList();

            var modelo = new DashboardViewModel
            {
                NombreUsuario = User.Identity?.Name ?? string.Empty,
                EnviosPendientes = pendientes.Count,
                EnviosPendientesLista = pendientes.Select(_mapper.Map<EnvioListadoViewModel>).ToList()
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
