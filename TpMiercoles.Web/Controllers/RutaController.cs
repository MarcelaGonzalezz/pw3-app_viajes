using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Ruta;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    [Authorize]
    public class RutaController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IRutaServicio _rutaServicio;
        private readonly ISesionWeb _sesionWeb;

        public RutaController(IMapper mapper, IRutaServicio rutaServicio, ISesionWeb sesionWeb)
        {
            _mapper = mapper;
            _rutaServicio = rutaServicio;
            _sesionWeb = sesionWeb;
        }
        [HttpGet]
        public IActionResult Crear()
        {
            var vm = new CrearRutaViewModel
            {
                FechaInicio = DateTime.Today
            };
            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> Crear(CrearRutaViewModel ruta)
        {
            if (!ModelState.IsValid) return View(ruta);
            int idUsuario = _sesionWeb.UsuarioIdRequerido;
            CrearRutaDTO rutaDTO = _mapper.Map<CrearRutaDTO>(ruta);
            try
            {
                await _rutaServicio.CrearRutaAsync(idUsuario,rutaDTO);
                return RedirectToAction("Index", "Home");
            }
            catch (ValidacionRutaException e)
            {
                ModelState.AddModelError("Error al crear la ruta: ", e.Message);
                return View(ruta);
            }


        }
    }
}
