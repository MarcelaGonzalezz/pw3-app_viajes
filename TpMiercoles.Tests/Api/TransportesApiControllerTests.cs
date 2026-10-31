using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TpMiercoles.Api.Controllers;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;

namespace TpMiercoles.Tests.Api
{
    // Verifica que cada endpoint conteste el codigo HTTP que corresponde:
    // 200 cuando sale bien, 404 si no existe o es de otro conductor,
    // 409 si la transicion no corresponde y 401 sin sesion.
    public class TransportesApiControllerTests
    {
        private const int ConductorId = 5;

        private readonly Mock<ITransporteServicio> _servicioMock = new();

        [Fact]
        public async Task Listar_ConSesion_Devuelve200ConLosTransportes()
        {
            List<TransporteListadoDTO> transportes = [new() { AsignacionId = 1, ViajeId = 10 }];
            _servicioMock.Setup(s => s.ListarAsync(ConductorId, null)).ReturnsAsync(transportes);

            var controlador = CrearControlador(ConductorId);

            var resultado = await controlador.Listar(null);

            var ok = Assert.IsType<OkObjectResult>(resultado.Result);
            Assert.Same(transportes, ok.Value);
        }

        [Fact]
        public async Task Listar_ConFiltroDeEstado_SeLoPasaAlServicio()
        {
            _servicioMock
                .Setup(s => s.ListarAsync(ConductorId, EstadoEnvio.EnCamino))
                .ReturnsAsync([]);

            var controlador = CrearControlador(ConductorId);

            await controlador.Listar(EstadoEnvio.EnCamino);

            _servicioMock.Verify(s => s.ListarAsync(ConductorId, EstadoEnvio.EnCamino), Times.Once);
        }

        [Fact]
        public async Task Listar_SinSesion_Devuelve401()
        {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Listar(null);

            Assert.IsType<UnauthorizedResult>(resultado.Result);
        }

        [Fact]
        public async Task Obtener_CuandoExisteYEsDelConductor_Devuelve200()
        {
            TransporteDetalleDTO detalle = new() { AsignacionId = 1 };
            _servicioMock.Setup(s => s.ObtenerDetalleAsync(1, ConductorId)).ReturnsAsync(detalle);

            var controlador = CrearControlador(ConductorId);

            var resultado = await controlador.Obtener(1);

            var ok = Assert.IsType<OkObjectResult>(resultado.Result);
            Assert.Same(detalle, ok.Value);
        }

        [Fact]
        public async Task Obtener_CuandoNoExisteOEsDeOtroConductor_Devuelve404()
        {
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(1, ConductorId))
                .ReturnsAsync((TransporteDetalleDTO?)null);

            var controlador = CrearControlador(ConductorId);

            var resultado = await controlador.Obtener(1);

            Assert.IsType<NotFoundResult>(resultado.Result);
        }

        [Theory]
        [InlineData("retirar", EstadoEnvio.Retirado)]
        [InlineData("en-camino", EstadoEnvio.EnCamino)]
        [InlineData("entregar", EstadoEnvio.Entregado)]
        public async Task Avanzar_CuandoLaTransicionEsValida_Devuelve200ConElTransporteActualizado(
            string paso, EstadoEnvio destino)
        {
            TransporteListadoDTO actualizado = new() { AsignacionId = 1, Estado = destino };
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, ConductorId, destino))
                .ReturnsAsync(ResultadoOperacionTransporte.Exitoso(actualizado));

            var controlador = CrearControlador(ConductorId);

            IActionResult resultado = await Ejecutar(controlador, paso, 1);

            var ok = Assert.IsType<OkObjectResult>(resultado);
            Assert.Same(actualizado, ok.Value);
        }

        [Theory]
        [InlineData("retirar")]
        [InlineData("en-camino")]
        [InlineData("entregar")]
        public async Task Avanzar_CuandoNoExisteOEsDeOtroConductor_Devuelve404(string paso)
        {
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, ConductorId, It.IsAny<EstadoEnvio>()))
                .ReturnsAsync(ResultadoOperacionTransporte.NoExiste());

            var controlador = CrearControlador(ConductorId);

            IActionResult resultado = await Ejecutar(controlador, paso, 1);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Theory]
        [InlineData("retirar")]
        [InlineData("en-camino")]
        [InlineData("entregar")]
        public async Task Avanzar_CuandoLaTransicionNoCorresponde_Devuelve409(string paso)
        {
            _servicioMock
                .Setup(s => s.AvanzarAsync(1, ConductorId, It.IsAny<EstadoEnvio>()))
                .ReturnsAsync(ResultadoOperacionTransporte.EnConflicto("No corresponde."));

            var controlador = CrearControlador(ConductorId);

            IActionResult resultado = await Ejecutar(controlador, paso, 1);

            var conflicto = Assert.IsType<ConflictObjectResult>(resultado);
            Assert.Contains("No corresponde.", conflicto.Value!.ToString());
        }

        [Theory]
        [InlineData("retirar")]
        [InlineData("en-camino")]
        [InlineData("entregar")]
        public async Task Avanzar_SinSesion_Devuelve401(string paso)
        {
            var controlador = CrearControladorSinSesion();

            IActionResult resultado = await Ejecutar(controlador, paso, 1);

            Assert.IsType<UnauthorizedResult>(resultado);
            _servicioMock.Verify(
                s => s.AvanzarAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<EstadoEnvio>()),
                Times.Never);
        }

        private static Task<IActionResult> Ejecutar(TransportesController controlador, string paso, int id)
            => paso switch
            {
                "retirar" => controlador.Retirar(id),
                "en-camino" => controlador.MarcarEnCamino(id),
                "entregar" => controlador.Entregar(id),
                _ => throw new ArgumentOutOfRangeException(nameof(paso))
            };

        private TransportesController CrearControlador(int usuarioId)
        {
            ClaimsPrincipal usuario = new(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())],
                "Cookies"));

            return ArmarControlador(usuario);
        }

        private TransportesController CrearControladorSinSesion()
            => ArmarControlador(new ClaimsPrincipal(new ClaimsIdentity()));

        private TransportesController ArmarControlador(ClaimsPrincipal usuario)
        {
            return new TransportesController(_servicioMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = usuario }
                }
            };
        }
    }
}
