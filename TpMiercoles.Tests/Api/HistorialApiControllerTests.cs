using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TpMiercoles.Api.Controllers;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Historial;

namespace TpMiercoles.Tests.Api {
    // Verifica el endpoint GET /api/historial: 200 con el historial del usuario,
    // los filtros llegan al servicio, 400 si "desde" es posterior a "hasta" y 401 sin sesion.
    public class HistorialApiControllerTests {
        private const int UsuarioId = 5;
        private static readonly DateTime Desde = new(2026, 9, 1);
        private static readonly DateTime Hasta = new(2026, 9, 30);

        private readonly Mock<IHistorialServicio> _servicioMock = new();

        [Fact]
        public async Task Consultar_SinFiltros_Devuelve200ConElHistorial() {
            HistorialDTO historial = new() { ViajesRealizados = 3, EnviosTransportados = 5, GananciaEfectiva = 12000m };
            _servicioMock
                .Setup(s => s.ObtenerAsync(UsuarioId, null, null, null, null))
                .ReturnsAsync(historial);

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Consultar(null, null, null, null);

            var ok = Assert.IsType<OkObjectResult>(resultado.Result);
            Assert.Same(historial, ok.Value);
        }

        [Fact]
        public async Task Consultar_ConFiltros_SeLosPasaAlServicio() {
            _servicioMock
                .Setup(s => s.ObtenerAsync(UsuarioId, TipoOperacionHistorial.ViajeRealizado,
                    EstadoActividadViaje.ACTIVO, Desde, Hasta))
                .ReturnsAsync(new HistorialDTO());

            var controlador = CrearControlador(UsuarioId);

            await controlador.Consultar(TipoOperacionHistorial.ViajeRealizado, EstadoActividadViaje.ACTIVO, Desde, Hasta);

            _servicioMock.Verify(s => s.ObtenerAsync(UsuarioId, TipoOperacionHistorial.ViajeRealizado,
                EstadoActividadViaje.ACTIVO, Desde, Hasta), Times.Once);
        }

        [Fact]
        public async Task Consultar_DesdePosteriorAHasta_Devuelve400SinConsultarElServicio() {
            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Consultar(null, null, Hasta, Desde);

            var badRequest = Assert.IsType<BadRequestObjectResult>(resultado.Result);
            Assert.Contains("desde", Assert.IsType<string>(badRequest.Value));
            _servicioMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Consultar_DesdeIgualAHasta_EsValidoParaFiltrarUnSoloDia() {
            _servicioMock
                .Setup(s => s.ObtenerAsync(UsuarioId, null, null, Desde, Desde))
                .ReturnsAsync(new HistorialDTO());

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Consultar(null, null, Desde, Desde);

            Assert.IsType<OkObjectResult>(resultado.Result);
        }

        [Fact]
        public async Task Consultar_SinSesion_Devuelve401SinConsultarElServicio() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Consultar(null, null, null, null);

            Assert.IsType<UnauthorizedResult>(resultado.Result);
            _servicioMock.VerifyNoOtherCalls();
        }

        private HistorialController CrearControlador(int usuarioId) {
            ClaimsPrincipal usuario = new(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())],
                "Cookies"));

            return ArmarControlador(usuario);
        }

        private HistorialController CrearControladorSinSesion()
            => ArmarControlador(new ClaimsPrincipal(new ClaimsIdentity()));

        private HistorialController ArmarControlador(ClaimsPrincipal usuario) {
            return new HistorialController(_servicioMock.Object) {
                ControllerContext = new ControllerContext {
                    HttpContext = new DefaultHttpContext { User = usuario }
                }
            };
        }
    }
}