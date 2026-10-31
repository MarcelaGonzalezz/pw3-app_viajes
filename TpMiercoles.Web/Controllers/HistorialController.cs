using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Historial;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    [Authorize]
    public class HistorialController : Controller
    {
        private readonly IHistorialServicio _servicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;

        public HistorialController(IHistorialServicio servicio, ISesionWeb sesionWeb, IMapper mapper)
        {
            _servicio = servicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
        }

        // GET /historial
        // GET /historial?tipo=ViajeRealizado&desde=2026-09-01&hasta=2026-09-30
        [HttpGet("/historial")]
        public async Task<IActionResult> Index(TipoOperacionHistorial? tipo, DateTime? desde, DateTime? hasta)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            string? error = null;
            if (desde is not null && hasta is not null && desde > hasta)
            {
                error = "La fecha \"desde\" no puede ser posterior a la fecha \"hasta\". Mostramos todo el historial.";
                desde = null;
                hasta = null;
            }

            // El servicio solo trae operaciones del usuario logueado: no hay forma de pedir las de otro.
            HistorialDTO historial = await _servicio.ObtenerAsync(usuarioId.Value, tipo, desde, hasta);

            HistorialViewModel modelo = new()
            {
                Filtro = tipo,
                Desde = desde,
                Hasta = hasta,
                Error = error,
                ViajesRealizados = historial.ViajesRealizados,
                EnviosTransportados = historial.EnviosTransportados,
                GananciaEfectiva = historial.GananciaEfectiva,
                TotalOperaciones = historial.TotalOperaciones,
                TotalEnviosEntregados = historial.TotalEnviosEntregados,
                TotalEnviosCancelados = historial.TotalEnviosCancelados,
                Items = historial.Items.Select(_mapper.Map<HistorialItemViewModel>).ToList()
            };

            return View(modelo);
        }
    }
}
