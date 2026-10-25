using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;
using static System.Collections.Specialized.BitVector32;

namespace TpMiercoles.Web.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IUsuarioServicio _usuarioServicio;
        private readonly ISesionWeb _sesionWeb;
        public UsuarioController(IUsuarioServicio usuarioServicio, IMapper mapper, ISesionWeb sesionWeb)
        {
            _usuarioServicio = usuarioServicio;
            _mapper = mapper;
            _sesionWeb = sesionWeb;
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
            }
            catch (EmailExistenteException e)
            {
                ModelState.AddModelError(nameof(usuario.Email), e.Message);
                return View(usuario);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginUsuarioViewModel usuario)
        {
            if (!ModelState.IsValid)
            {
                return View(usuario);
            }
            UsuarioSesionDTO? usuarioSesionDTO = await _usuarioServicio.LoginAsync(_mapper.Map<UsuarioLoginDTO>(usuario));

            if (usuarioSesionDTO is null)
            {
                ModelState.AddModelError("", "Email o contraseña incorrectos");
                return View(usuario);
            }
            await _sesionWeb.IniciarAsync(usuarioSesionDTO, usuario.Recordarme);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CerrarSesion()
        {
            await _sesionWeb.CerrarAsync();
            return RedirectToAction("Login", "Usuario");
        }
    }
}
