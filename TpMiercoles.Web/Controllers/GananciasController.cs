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

        public GananciasController(
            IGananciasServicio servicio,
            ISesionWeb sesionWeb,
            IMapper mapper,
            TimeProvider reloj)
        {
            _servicio = servicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
            _reloj = reloj;
        }

        [HttpGet("/ganancias")]
        public async Task<IActionResult> Index( int? anio, int? mes, CriterioOptimizacion criterio = CriterioOptimizacion.MayorGanancia,
            bool limitarDesvio = false, CancellationToken ct = default)
        {
            int? conductorId = _sesionWeb.UsuarioId;
            if (conductorId is null)
            {
                return Challenge();
            }

            DateTimeOffset hoy = _reloj.GetLocalNow();
            string? error = null;

            if (mes is < 1 or > 12 || anio is < 2000 or > 2100 || !Enum.IsDefined(criterio))
            {
                error = "El mes, el año o el criterio no son válidos. Mostramos el mes actual con el criterio Mayor ganancia.";
                anio = null;
                mes = null;
                criterio = CriterioOptimizacion.MayorGanancia;
            }

            double? limite = limitarDesvio ? GananciasViewModel.DesvioDelSistemaKm : null;

            GananciasMensualDTO ganancias = await _servicio.CalcularAsync(
                conductorId.Value,
                anio ?? hoy.Year,
                mes ?? hoy.Month,
                criterio,
                limite,
                ct);

            GananciasViewModel modelo = _mapper.Map<GananciasViewModel>(ganancias);
            modelo.Error = error;

            return View(modelo);
        }
    }
}
