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
        public async Task<IActionResult> Index(EstadoEnvio? estado)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            List<EnvioListadoDTO> todos = await _servicio.ListarAsync(usuarioId.Value, null);

            List<EnvioListadoDTO> filtrados = estado is null
                ? todos
                : todos.Where(e => e.Estado == estado.Value).ToList();

            var modelo = new MisEnviosViewModel
            {
                Filtro = estado,
                Envios = filtrados.Select(_mapper.Map<EnvioListadoViewModel>).ToList(),
                TotalTodos = todos.Count,
                TotalBuscando = todos.Count(e => e.Estado == EstadoEnvio.BuscandoConductor),
                TotalAsignados = todos.Count(e => e.Estado == EstadoEnvio.ConductorAsignado),
                TotalEnCamino = todos.Count(e => e.Estado == EstadoEnvio.Retirado || e.Estado == EstadoEnvio.EnCamino),
                TotalEntregados = todos.Count(e => e.Estado == EstadoEnvio.Entregado),
                TotalCancelados = todos.Count(e => e.Estado == EstadoEnvio.Cancelado)
            };

            return View(modelo);
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

            int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _servicio.CrearAsync(_mapper.Map<EnvioDTO>(modelo), usuarioId);

            TempData["Mensaje"] = "Envío publicado correctamente";
            return RedirectToAction("Index");
        }
    }
}
