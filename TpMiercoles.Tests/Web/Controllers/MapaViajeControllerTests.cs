using Microsoft.AspNetCore.Mvc;
using Moq;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Mapa;
using TpMiercoles.Servicios.Viajes;
using TpMiercoles.Web.Controllers;
using TpMiercoles.Web.Infraestructura;

namespace TpMiercoles.Tests.Web.Controllers {
    public class MapaViajeControllerTests {
        private const int UsuarioId = 5;
        private const int ViajeId = 13;

        private readonly Mock<IMapaViajeServicio> _servicioMock = new();
        private readonly Mock<ISesionWeb> _sesionMock = new();
        private readonly MapaViajeController _controller;

        public MapaViajeControllerTests() {
            _sesionMock.SetupGet(s => s.UsuarioId).Returns(UsuarioId);
            _controller = new MapaViajeController(_servicioMock.Object, _sesionMock.Object);
        }

        [Fact]
        public async Task Obtener_SinSesion_DevuelveUnauthorizedYNoConsultaElServicio() {
            _sesionMock.SetupGet(s => s.UsuarioId).Returns((int?)null);

            IActionResult resultado = await _controller.Obtener(ViajeId, CancellationToken.None);

            Assert.IsType<UnauthorizedResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Obtener_CuandoElViajeNoExisteONoEsDelUsuario_DevuelveNotFound() {
            _servicioMock
                .Setup(s => s.ObtenerAsync(ViajeId, UsuarioId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MapaViajeDTO?)null);

            IActionResult resultado = await _controller.Obtener(ViajeId, CancellationToken.None);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Obtener_CuandoElViajeEsDelUsuario_DevuelveOkConElMapa() {
            var mapa = new MapaViajeDTO {
                Puntos = [new PuntoMapaDTO { Tipo = TipoPuntoMapa.Origen, Texto = "Texalar" }],
                DistanciaBaseKm = 2.8
            };
            _servicioMock
                .Setup(s => s.ObtenerAsync(ViajeId, UsuarioId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mapa);

            IActionResult resultado = await _controller.Obtener(ViajeId, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(resultado);
            Assert.Same(mapa, ok.Value);
        }

        [Fact]
        public async Task Obtener_ConsultaElMapaConElUsuarioLogueado() {
            await _controller.Obtener(ViajeId, CancellationToken.None);

            _servicioMock.Verify(s => s.ObtenerAsync(ViajeId, UsuarioId, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}