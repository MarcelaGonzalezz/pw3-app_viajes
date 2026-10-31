using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Web.Models;

namespace TpMiercoles.Web.Controllers {

    [Authorize]
    public class RecorridoController : Controller {
        private readonly IRecorridoServicio _recorridoServicio;
        private readonly IMapper _mapper;

        public RecorridoController(IRecorridoServicio recorridoServicio, IMapper mapper) {
            _recorridoServicio = recorridoServicio;
            _mapper = mapper;
        }
        [HttpPost("/api/recorrido")]
        public async Task<IActionResult> Calcular([FromBody] RecorridoViewModel modelo, CancellationToken ct) {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Elegí el origen y el destino de la lista de direcciones." });

            RecorridoDTO recorrido = new() {
                Origen = _mapper.Map<DireccionDTO>(modelo.Origen),
                Destino = _mapper.Map<DireccionDTO>(modelo.Destino),
                Paradas = _mapper.Map<List<ParadaDTO>>(modelo.Paradas)
            };

            try {
                ResultadoRuta resultado = await _recorridoServicio.CalcularAsync(recorrido, ct);

                return Ok(new {
                    distanciaKm = Math.Round(resultado.DistanciaKm, 1),
                    duracionMinutos = Math.Round(resultado.DuracionMinutos)
                });
            } catch (RutaNoEncontradaException) {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { error = "No se pudo calcular el recorrido en este momento." });
            }
        }
    }
}

