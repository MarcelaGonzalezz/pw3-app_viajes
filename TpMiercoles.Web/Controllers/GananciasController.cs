using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Ganancias;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    [Authorize]
    public class GananciasController : Controller
    {
        private readonly IGananciasServicio _servicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;
        private readonly TimeProvider _reloj;
        private readonly IPreferenciasDeGanancias _preferencias;

        public GananciasController(
            IGananciasServicio servicio,
            ISesionWeb sesionWeb,
            IMapper mapper,
            TimeProvider reloj,
            IPreferenciasDeGanancias preferencias)
        {
            _servicio = servicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
            _reloj = reloj;
            _preferencias = preferencias;
        }

        [HttpGet("/ganancias")]
        // criterio y desvioMaximoKm son null cuando se entra sin tocar el formulario (desde el menu
        // o el inicio): ahi se usa lo ultimo que eligio el usuario. Al enviar el formulario el
        // criterio siempre viene, y el checkbox destildado no manda nada, asi que null = false.
        public async Task<IActionResult> Index(int? anio, int? mes, CriterioOptimizacion? criterio = null,
            double? desvioMaximoKm = null, CancellationToken ct = default)
        {
            int? conductorId = _sesionWeb.UsuarioId;
            if (conductorId is null)
            {
                return Challenge();
            }

            DateTimeOffset hoy = _reloj.GetLocalNow();
            string? error = null;

            bool vieneDelFormulario = criterio is not null;
            PreferenciaDeGanancias guardada = vieneDelFormulario
                ? PreferenciaDeGanancias.PorDefecto
                : _preferencias.Leer(conductorId.Value) ?? PreferenciaDeGanancias.PorDefecto;

            CriterioOptimizacion criterioElegido = criterio ?? guardada.Criterio;
            double desvio = desvioMaximoKm ?? (vieneDelFormulario ? GananciasViewModel.TopeDesvioKm : guardada.DesvioMaximoKm);

            if (mes is < 1 or > 12 || anio is < 2000 or > 2100 || !Enum.IsDefined(criterioElegido) 
                || !double.IsFinite(desvio) || desvio < 1 || desvio > GananciasViewModel.TopeDesvioKm)
            {
                error = "El mes, el año, el criterio o el desvío no son válidos. Mostramos el mes actual con el criterio Mayor ganancia y 10 km de desvío máximo.";
                anio = null;
                mes = null;
                criterioElegido = CriterioOptimizacion.MayorGanancia;
                desvio = GananciasViewModel.TopeDesvioKm;
            }
            else if (vieneDelFormulario)
            {
                _preferencias.Guardar(conductorId.Value, new PreferenciaDeGanancias(criterioElegido, desvio));
            }

            GananciasMensualDTO ganancias = await _servicio.CalcularAsync(
                conductorId.Value,
                anio ?? hoy.Year,
                mes ?? hoy.Month,
                criterioElegido,
                desvio,
                ct);

            GananciasViewModel modelo = _mapper.Map<GananciasViewModel>(ganancias);
            modelo.Error = error;

            return View(modelo);
        }
    }
}
