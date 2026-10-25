using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Web.Infraestructura
{

    public interface ISesionWeb
    {
        Task IniciarAsync(UsuarioSesionDTO sesion, bool recordarme);
        Task CerrarAsync();
    }
    public class SesionWeb : ISesionWeb
    {
        private readonly IHttpContextAccessor _accessor;

        public SesionWeb(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public async Task IniciarAsync(UsuarioSesionDTO sesion, bool recordarme)
        {
            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sesion.Id.ToString()),
            new(ClaimTypes.Name, sesion.Nombre),
            new(ClaimTypes.Surname, sesion.Apellido),
            new(ClaimTypes.Email, sesion.Email)
        };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await _accessor.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = recordarme });
        }

        public Task CerrarAsync()
            => _accessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
