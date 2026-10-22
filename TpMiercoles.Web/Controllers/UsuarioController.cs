using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Web.Models;
using TpMiercoles.Entidades.Excepciones;

namespace TpMiercoles.Web.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IUsuarioServicio _usuarioServicio;

        public UsuarioController(IUsuarioServicio usuarioServicio, IMapper mapper)
        {
            _usuarioServicio = usuarioServicio;
            _mapper = mapper;
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registrar(RegistroUsuarioViewModel usuario)
        {
            if (!ModelState.IsValid)
            {
                return View(usuario);
            }
            try
            {
                UsuarioDTO usuarioDTO = _mapper.Map<UsuarioDTO>(usuario);
                await _usuarioServicio.RegistrarAsync(usuarioDTO);
                return RedirectToAction("Login");
            } catch (EmailExistenteException e)
            {
                ModelState.AddModelError(nameof(usuario.Email), e.Message);
                return View(usuario);
            }
        }
    }
}
