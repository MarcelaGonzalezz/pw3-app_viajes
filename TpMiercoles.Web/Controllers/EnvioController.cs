using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;
using System.Globalization;

namespace TpMiercoles.Web.Controllers
{
    [Authorize]
    public class EnvioController : Controller
    {
        private readonly IEnvioServicio _servicio;
        private readonly IMapper _mapper;
        private readonly ISesionWeb _sesionWeb;

        public EnvioController(IEnvioServicio servicio, IMapper mapper, ISesionWeb sesionWeb)
        {
            _servicio = servicio;
            _mapper = mapper;
            _sesionWeb = sesionWeb;
        }

        [HttpGet("/envios")]
        public async Task<IActionResult> Index(EstadoEnvio? estado, bool vencidos = false, string? texto = null, DateTime? desde = null, DateTime? hasta = null)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            List<EnvioListadoViewModel> todos = (await _servicio.ListarAsync(usuarioId.Value, null))
                .Select(_mapper.Map<EnvioListadoViewModel>)
                .ToList();

            IEnumerable<EnvioListadoViewModel> filtrados = vencidos
                ? todos.Where(e => e.EstaVencido)
                : estado is null
                    ? todos
                    : todos.Where(e => e.Estado == estado.Value && !e.EstaVencido);

            string? busqueda = texto?.Trim();
            if (!string.IsNullOrEmpty(busqueda))
            {
                filtrados = filtrados.Where(e => Contiene(e.Origen, busqueda) || Contiene(e.Destino, busqueda));
            }

            // el envio entra si su rango [FechaDesde, FechaHasta] se solapa con el pedido
            if (desde is not null)
            {
                filtrados = filtrados.Where(e => e.FechaHasta.Date >= desde.Value.Date);
            }

            if (hasta is not null)
            {
                filtrados = filtrados.Where(e => e.FechaDesde.Date <= hasta.Value.Date);
            }

            var modelo = new MisEnviosViewModel
            {
                Filtro = estado,
                SoloVencidos = vencidos,
                Texto = busqueda,
                Desde = desde,
                Hasta = hasta,
                Envios = filtrados.ToList(),
                TotalTodos = todos.Count,
                TotalBuscando = todos.Count(e => e.Estado == EstadoEnvio.BuscandoConductor && !e.EstaVencido),
                TotalVencidos = todos.Count(e => e.EstaVencido),
                TotalAsignados = todos.Count(e => e.Estado == EstadoEnvio.ConductorAsignado),
                TotalRetirados = todos.Count(e => e.Estado == EstadoEnvio.Retirado),
                TotalEnCamino = todos.Count(e => e.Estado == EstadoEnvio.EnCamino),
                TotalEntregados = todos.Count(e => e.Estado == EstadoEnvio.Entregado),
                TotalCancelados = todos.Count(e => e.Estado == EstadoEnvio.Cancelado)
            };

            return View(modelo);
        }

        [HttpGet("/envios/detalle/{id:int}")]
        public async Task<IActionResult> Detalle(int id)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            EnvioDetalleDTO? dto = await _servicio.ObtenerDetalleAsync(id, usuarioId.Value);
            if (dto is null)
            {
                return NotFound();
            }

            return View(_mapper.Map<EnvioDetalleViewModel>(dto));
        }

        [HttpPost("/envios/{id:int}/cancelar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, bool volverADetalle = false)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ResultadoOperacionEnvio resultado = await _servicio.CancelarAsync(id, usuarioId.Value);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            if (!resultado.EsExitoso)
            {
                TempData["Error"] = resultado.Error;
            }
            else
            {
                TempData["Mensaje"] = $"El envío #{id} fue cancelado correctamente.";
            }

            return volverADetalle
                ? RedirectToAction(nameof(Detalle), new { id })
                : RedirectToAction(nameof(Index));
        }

        [HttpGet("/envios/nuevo")]
        public IActionResult Nuevo()
        {
            return View(new NuevoEnvioViewModel());
        }

        [HttpPost("/envios/nuevo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nuevo(NuevoEnvioViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ResultadoOperacionEnvio resultado = await _servicio.CrearAsync(_mapper.Map<EnvioDTO>(modelo), usuarioId.Value);

            if (!resultado.EsExitoso)
            {
                ModelState.AddModelError(string.Empty, resultado.Error!);
                return View(modelo);
            }

            TempData["Mensaje"] = "Envío publicado correctamente";
            return RedirectToAction(nameof(Index));
        }

        // sin importar mayusculas ni tildes ("moron" encuentra "Morón")
        private static bool Contiene(string texto, string busqueda)
            => CultureInfo.InvariantCulture.CompareInfo.IndexOf(texto, busqueda, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }
}
