using TpMiercoles.Servicios.DTOs.Ganancias;
using TpMiercoles.Web.Models;
using System.Globalization;

namespace TpMiercoles.Web.Infraestructura
{
    // Lo que el conductor eligio por ultima vez en Potencial de ganancias.
    // El inicio lo usa para que la tarjeta "Potencial del mes" muestre el mismo numero.
    public sealed record PreferenciaDeGanancias(CriterioOptimizacion Criterio, double DesvioMaximoKm)
    {
        public static readonly PreferenciaDeGanancias PorDefecto =
            new(CriterioOptimizacion.MayorGanancia, GananciasViewModel.TopeDesvioKm);

        public string Descripcion
        {
            get
            {
                string criterio = Criterio switch
                {
                    CriterioOptimizacion.MenorDesvio => "menor desvío",
                    CriterioOptimizacion.Equilibrada => "criterio equilibrado",
                    _ => "mayor ganancia"
                };

                return $"Con {criterio}, desvío hasta {DesvioMaximoKm:0.#} km";
            }
        }
    }

    public interface IPreferenciasDeGanancias
    {
        // null si el usuario todavia no eligio nada en este navegador
        PreferenciaDeGanancias? Leer(int usuarioId);
        void Guardar(int usuarioId, PreferenciaDeGanancias preferencia);
    }

    // Se guarda en una cookie con el id del usuario, para que si otra persona inicia
    // sesion en el mismo navegador no herede la eleccion. Formato: "usuarioId|criterio|0 o 1".
    public sealed class PreferenciasDeGananciasEnCookie : IPreferenciasDeGanancias
    {
        private const string NombreCookie = ".TpMiercoles.Ganancias.v2";

        private readonly IHttpContextAccessor _accessor;

        public PreferenciasDeGananciasEnCookie(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public PreferenciaDeGanancias? Leer(int usuarioId)
        {
            string? valor = _accessor.HttpContext?.Request.Cookies[NombreCookie];
            string[] partes = valor?.Split('|') ?? [];

            if (partes.Length != 3
                || !int.TryParse(partes[0], out int dueno) || dueno != usuarioId
                || !int.TryParse(partes[1], out int criterio)
                || !Enum.IsDefined((CriterioOptimizacion)criterio)
                || !double.TryParse(partes[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double km)
                || !double.IsFinite(km)
                || km < 1 || km > GananciasViewModel.TopeDesvioKm)
            {
                return null;
            }

            return new PreferenciaDeGanancias((CriterioOptimizacion)criterio, km);
        }

        public void Guardar(int usuarioId, PreferenciaDeGanancias preferencia)
        {
            _accessor.HttpContext?.Response.Cookies.Append(
                NombreCookie,
                $"{usuarioId}|{(int)preferencia.Criterio}|{(preferencia.DesvioMaximoKm.ToString(CultureInfo.InvariantCulture))}",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                });
        }
    }
}
