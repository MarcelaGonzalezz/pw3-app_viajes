using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Ruta;
using TpMiercoles.Servicios.Excepciones;
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
                await _rutaServicio.CrearRutaAsync(idUsuario, rutaDTO);
                return RedirectToAction("Index", "Home");
            }
            catch (ValidacionRutaException e)
            {
                ModelState.AddModelError("Error al crear la ruta: ", e.Message);
                return View(ruta);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Obtener()
        {
            int usuarioId = _sesionWeb.UsuarioIdRequerido;
            string nombreUsuario = User.Identity?.Name;
            var rutasDTO = await _rutaServicio.ObtenerRutasDeUnUsuarioAsync(usuarioId);
            var rutasVm = _mapper.Map<List<RutaViewModel>>(rutasDTO);
            return View(new ObtenerRutasViewModel { Rutas = rutasVm, NombreUsuario = nombreUsuario });
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            int usuarioId = _sesionWeb.UsuarioIdRequerido;

            try
            {
                var rutaDto = await _rutaServicio.ObtenerDetalleAsync(id, usuarioId);
                return View(rutaDto);
            }
            catch (RutaNoEncontradaException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            int usuarioId = _sesionWeb.UsuarioIdRequerido;

            try
            {
                EditarRutaDTO dto = await _rutaServicio.ObtenerParaEditarAsync(id, usuarioId);
                var modelo = _mapper.Map<EditarRutaViewModel>(dto);
                return View(modelo);
            }
            catch (RutaNoEncontradaException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, EditarRutaViewModel ruta)
        {
            if (!ModelState.IsValid)
            {
                return View(ruta);
            }

            int usuarioId = _sesionWeb.UsuarioIdRequerido;
            EditarRutaDTO dto = _mapper.Map<EditarRutaDTO>(ruta);

            try
            {
                await _rutaServicio.EditarRutaAsync(usuarioId, id, dto);
                TempData["Mensaje"] = "La ruta fue modificada exitosamente.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
            catch (RutaNoEncontradaException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ValidacionRutaException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(ruta);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id, bool volverADetalle = false)
        {
            int usuarioId = _sesionWeb.UsuarioIdRequerido;
            try
            {
                await _rutaServicio.DesactivarRutaAsync(id, usuarioId);
                TempData["Mensaje"] = "La ruta fue desactivada correctamente. Los viajes comprometidos siguen en pie.";
            }
            catch (RutaNoEncontradaException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            return volverADetalle
                ? RedirectToAction(nameof(Detalle), new { id })
                : RedirectToAction(nameof(Obtener));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id, bool volverADetalle = false)
        {
            int usuarioId = _sesionWeb.UsuarioIdRequerido;
            try
            {
                await _rutaServicio.ActivarRutaAsync(id, usuarioId);
                TempData["Mensaje"] = "La ruta fue reactivada correctamente y volverá a buscar oportunidades.";
            }
            catch (RutaNoEncontradaException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            return volverADetalle
                ? RedirectToAction(nameof(Detalle), new { id })
                : RedirectToAction(nameof(Obtener));
        }
    }
}
