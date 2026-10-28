using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers {
    [Authorize]
    public class EnvioController:Controller {
        private readonly IEnvioServicio _servicio;
        private readonly IMapper _mapper;

        public EnvioController(IEnvioServicio servicio, IMapper mapper) {
            _servicio = servicio;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult Nuevo() {
            // Pasamos los valores del enum convertidos a un SelectList limpio
            ViewBag.Tamanios = new SelectList(Enum.GetValues(typeof(TamanioEnvio))
                .Cast<TamanioEnvio>()
                .Select(e => new { Id = (int)e, Nombre = e.ToString() }), "Id", "Nombre");

            return View(new NuevoEnvioViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nuevo(NuevoEnvioViewModel modelo) {
            if (!ModelState.IsValid)
                return View(modelo);

            int usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _servicio.CrearAsync(_mapper.Map<EnvioDTO>(modelo), usuarioId);

            TempData["Mensaje"] = "Envío publicado correctamente";
            return RedirectToAction("Index", "Home");
        }
    }
}
