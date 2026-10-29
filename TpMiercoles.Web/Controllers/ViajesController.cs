using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Web.Infraestructura;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers
{
    [Authorize]
    public class ViajesController : Controller
    {
        private readonly IViajeServicio _viajeServicio;
        private readonly ISesionWeb _sesionWeb;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;

        public ViajesController(
            IViajeServicio viajeServicio,
            ISesionWeb sesionWeb,
            IMapper mapper,
            IHttpClientFactory httpClientFactory)
        {
            _viajeServicio = viajeServicio;
            _sesionWeb = sesionWeb;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
        }

        // GET /viajes  (opcional ?estado=ACTIVO | INACTIVO; sin parametro trae todos)
        [HttpGet("/viajes")]
        public async Task<IActionResult> Index(EstadoActividadViaje? estado)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            HttpClient clienteApi = _httpClientFactory.CreateClient("ViajesApi");
            using HttpRequestMessage solicitud = new(HttpMethod.Get, "api/viajes");

            // HttpClient no copia automáticamente la cookie del navegador.
            // La reenviamos para que la API pueda identificar al usuario.
            string cookie = Request.Headers.Cookie.ToString();
            if (!string.IsNullOrWhiteSpace(cookie))
            {
                solicitud.Headers.TryAddWithoutValidation("Cookie", cookie);
            }

            using HttpResponseMessage respuesta = await clienteApi.SendAsync(solicitud);

            if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return Challenge();
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                return StatusCode((int)respuesta.StatusCode,
                    "No se pudo consultar la API de viajes.");
            }

            List<ViajeListadoDTO> todos =
                await respuesta.Content.ReadFromJsonAsync<List<ViajeListadoDTO>>() ?? [];

            List<ViajeListadoDTO> filtrados = estado is null
                ? todos
                : todos.Where(v => v.Estado == estado.Value).ToList();

            MisViajesViewModel modelo = new()
            {
                Filtro = estado,
                Viajes = filtrados.Select(_mapper.Map<ViajeListadoViewModel>).ToList(),
                TotalActivos = todos.Count(v => v.Estado == EstadoActividadViaje.ACTIVO),
                TotalInactivos = todos.Count(v => v.Estado == EstadoActividadViaje.INACTIVO)
            };

            return View(modelo);
        }

        // GET /viajes/{id}
        [HttpGet("/viajes/{id:int}")]
        public async Task<IActionResult> Detalle(int id)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ViajeDetalleDTO? viaje = await _viajeServicio.ObtenerDetalleAsync(id, usuarioId.Value);

            // El servicio devuelve null tanto si el viaje no existe como si no es
            // del usuario: respondemos 404 en los dos casos para no delatar que existe.
            if (viaje is null)
            {
                return NotFound();
            }

            return View(_mapper.Map<ViajeDetalleViewModel>(viaje));
        }

        // GET /viajes/{id}/editar
        [HttpGet("/viajes/{id:int}/editar")]
        public async Task<IActionResult> Editar(int id)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ViajeEdicionDTO? viaje = await _viajeServicio.ObtenerParaEditarAsync(id, usuarioId.Value);

            if (viaje is null)
            {
                return NotFound();
            }

            return View(_mapper.Map<ViajeEdicionViewModel>(viaje));
        }

        // El formulario HTML no puede mandar PUT, asi que postea aca.
        [HttpPost("/viajes/{id:int}/editar")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> EditarDesdeFormulario(int id, ViajeEdicionViewModel modelo)
            => GuardarEdicion(id, modelo);

        // PUT /viajes/{id} : el mismo endpoint, con el verbo de la historia de usuario.
        [HttpPut("/viajes/{id:int}")]
        public Task<IActionResult> Actualizar(int id, ViajeEdicionViewModel modelo)
            => GuardarEdicion(id, modelo);

        // POST /viajes/{id}/desactivar : baja logica.
        [HttpPost("/viajes/{id:int}/desactivar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id, bool volverADetalle = false)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ResultadoOperacionViaje resultado = await _viajeServicio.DesactivarAsync(id, usuarioId.Value);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            TempData["Mensaje"] = resultado.PaquetesAfectados == 0
                ? $"El viaje #{id} quedó inactivo y dejó de buscar oportunidades."
                : $"El viaje #{id} quedó inactivo. Sus {resultado.PaquetesAfectados} paquete(s) ya asignado(s) " +
                  "siguen en pie: hay que entregarlos igual.";

            return volverADetalle
                ? RedirectToAction(nameof(Detalle), new { id })
                : RedirectToAction(nameof(Index));
        }

        // POST /viajes/{id}/activar : vuelve a ponerlo a buscar oportunidades.
        [HttpPost("/viajes/{id:int}/activar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activar(int id, bool volverADetalle = false)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            ResultadoOperacionViaje resultado = await _viajeServicio.ActivarAsync(id, usuarioId.Value);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            TempData["Mensaje"] = $"El viaje #{id} volvió a estar activo y busca oportunidades de nuevo.";

            return volverADetalle
                ? RedirectToAction(nameof(Detalle), new { id })
                : RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> GuardarEdicion(int id, ViajeEdicionViewModel modelo)
        {
            int? usuarioId = _sesionWeb.UsuarioId;
            if (usuarioId is null)
            {
                return Challenge();
            }

            modelo.Id = id;

            if (!ModelState.IsValid)
            {
                return View("Editar", await RellenarContexto(id, usuarioId.Value, modelo));
            }

            ViajeEdicionDTO datos = _mapper.Map<ViajeEdicionDTO>(modelo);
            ResultadoOperacionViaje resultado = await _viajeServicio.ActualizarAsync(id, usuarioId.Value, datos);

            if (resultado.NoEncontrado)
            {
                return NotFound();
            }

            if (!resultado.EsExitoso)
            {
                foreach (string error in resultado.Errores)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View("Editar", await RellenarContexto(id, usuarioId.Value, modelo));
            }

            TempData["Mensaje"] = $"Guardamos los cambios del viaje #{id}.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // El formulario solo postea los campos editables: volvemos a buscar el resto
        // (origen, destino, estado, paquetes) para poder re-dibujar la pantalla.
        private async Task<ViajeEdicionViewModel> RellenarContexto(int id, int usuarioId, ViajeEdicionViewModel modelo)
        {
            ViajeEdicionDTO? actual = await _viajeServicio.ObtenerParaEditarAsync(id, usuarioId);

            if (actual is not null)
            {
                modelo.DireccionOrigen = actual.DireccionOrigen;
                modelo.DireccionDestino = actual.DireccionDestino;
                modelo.Estado = actual.Estado;
                modelo.CantidadPaquetes = actual.CantidadPaquetes;
            }

            return modelo;
        }
    }
}
