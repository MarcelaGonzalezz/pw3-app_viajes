using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TpMiercoles.Api.Controllers;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.Excepciones;

namespace TpMiercoles.Tests.Api {
    // Verifica que cada endpoint de /api/rutas/{rutaId}/oportunidades conteste el codigo HTTP
    // que corresponde: 200/201 cuando sale bien, 400 si el desvio es negativo, 404 si la ruta
    // o el envio no existen, 409 si el envio ya no se puede aceptar, 502 si falla el servicio
    // de rutas y 401 sin sesion.
    public class OportunidadesApiControllerTests {
        private const int UsuarioId = 5;
        private const int RutaId = 1;
        private const int EnvioId = 20;
        private static readonly DateTime Fecha = new(2026, 11, 9);

        private readonly Mock<IOportunidadServicio> _servicioMock = new();

        #region GET /api/rutas/{rutaId}/oportunidades

        [Fact]
        public async Task Listar_ConSesion_Devuelve200ConLaSalidaYSusOportunidades() {
            OportunidadesDeRutaDTO oportunidades = new() { RutaId = RutaId };
            _servicioMock
                .Setup(s => s.ListarAsync(RutaId, UsuarioId, null, CriterioOrdenOportunidad.Ganancia, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(oportunidades);

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Listar(RutaId);

            var ok = Assert.IsType<OkObjectResult>(resultado.Result);
            Assert.Same(oportunidades, ok.Value);
        }

        [Fact]
        public async Task Listar_ConFechaOrdenYDesvio_SeLosPasaAlServicio() {
            _servicioMock
                .Setup(s => s.ListarAsync(RutaId, UsuarioId, Fecha, CriterioOrdenOportunidad.Desvio, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OportunidadesDeRutaDTO());

            var controlador = CrearControlador(UsuarioId);

            await controlador.Listar(RutaId, Fecha, CriterioOrdenOportunidad.Desvio, 5);

            _servicioMock.Verify(s => s.ListarAsync(RutaId, UsuarioId, Fecha, CriterioOrdenOportunidad.Desvio, 5,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Listar_DesvioNegativo_Devuelve400SinConsultarElServicio() {
            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Listar(RutaId, desvioMaximoKm: -1);

            Assert.IsType<BadRequestObjectResult>(resultado.Result);
            _servicioMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Listar_RutaInexistenteDeOtroUsuarioOSinSalida_Devuelve404() {
            _servicioMock
                .Setup(s => s.ListarAsync(RutaId, UsuarioId, null, CriterioOrdenOportunidad.Ganancia, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((OportunidadesDeRutaDTO?)null);

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Listar(RutaId);

            Assert.IsType<NotFoundResult>(resultado.Result);
        }

        [Fact]
        public async Task Listar_FallaElServicioDeRutas_Devuelve502() {
            _servicioMock
                .Setup(s => s.ListarAsync(RutaId, UsuarioId, null, CriterioOrdenOportunidad.Ganancia, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RutaNoEncontradaException("OpenRouteService no respondió."));

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Listar(RutaId);

            var error = Assert.IsType<ObjectResult>(resultado.Result);
            Assert.Equal(StatusCodes.Status502BadGateway, error.StatusCode);
        }

        [Fact]
        public async Task Listar_SinSesion_Devuelve401() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Listar(RutaId);

            Assert.IsType<UnauthorizedResult>(resultado.Result);
            _servicioMock.VerifyNoOtherCalls();
        }

        #endregion

        #region GET /api/rutas/{rutaId}/oportunidades/{envioId}

        [Fact]
        public async Task Obtener_CuandoExiste_Devuelve200ConElDetalle() {
            OportunidadDetalleDTO detalle = new();
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(RutaId, EnvioId, UsuarioId, Fecha, It.IsAny<CancellationToken>()))
                .ReturnsAsync(detalle);

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Obtener(RutaId, EnvioId, Fecha);

            var ok = Assert.IsType<OkObjectResult>(resultado.Result);
            Assert.Same(detalle, ok.Value);
        }

        [Fact]
        public async Task Obtener_RutaOEnvioInexistente_Devuelve404() {
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(RutaId, EnvioId, UsuarioId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((OportunidadDetalleDTO?)null);

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Obtener(RutaId, EnvioId);

            Assert.IsType<NotFoundResult>(resultado.Result);
        }

        [Fact]
        public async Task Obtener_FallaElServicioDeRutas_Devuelve502() {
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(RutaId, EnvioId, UsuarioId, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RutaNoEncontradaException("OpenRouteService no respondió."));

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Obtener(RutaId, EnvioId);

            var error = Assert.IsType<ObjectResult>(resultado.Result);
            Assert.Equal(StatusCodes.Status502BadGateway, error.StatusCode);
        }

        [Fact]
        public async Task Obtener_SinSesion_Devuelve401() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Obtener(RutaId, EnvioId);

            Assert.IsType<UnauthorizedResult>(resultado.Result);
            _servicioMock.VerifyNoOtherCalls();
        }

        #endregion

        #region POST /api/rutas/{rutaId}/oportunidades/{envioId}/aceptar

        [Fact]
        public async Task Aceptar_CuandoSePuede_Devuelve201ConLaUbicacionDelTransporte() {
            _servicioMock
                .Setup(s => s.AceptarAsync(RutaId, EnvioId, UsuarioId, Fecha, null))
                .ReturnsAsync(ResultadoOperacionOportunidad.Exitoso(asignacionId: 7, viajeId: 50));

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Aceptar(RutaId, EnvioId, Fecha);

            var creado = Assert.IsType<CreatedResult>(resultado);
            Assert.Equal("/api/transportes/7", creado.Location);
            Assert.Equal(7, Propiedad(creado.Value, "asignacionId"));
            Assert.Equal(50, Propiedad(creado.Value, "viajeId"));
        }

        [Fact]
        public async Task Aceptar_EnvioTomadoPorOtroConductor_Devuelve409ConElMotivo() {
            _servicioMock
                .Setup(s => s.AceptarAsync(RutaId, EnvioId, UsuarioId, null, null))
                .ReturnsAsync(ResultadoOperacionOportunidad.EnConflicto("El envío ya fue asignado a otro conductor."));

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Aceptar(RutaId, EnvioId);

            var conflicto = Assert.IsType<ConflictObjectResult>(resultado);
            Assert.Equal("El envío ya fue asignado a otro conductor.", Propiedad(conflicto.Value, "error"));
        }

        [Fact]
        public async Task Aceptar_RutaOEnvioInexistente_Devuelve404() {
            _servicioMock
                .Setup(s => s.AceptarAsync(RutaId, EnvioId, UsuarioId, null, null))
                .ReturnsAsync(ResultadoOperacionOportunidad.NoExiste());

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Aceptar(RutaId, EnvioId);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Aceptar_SinSesion_Devuelve401SinAceptarNada() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Aceptar(RutaId, EnvioId);

            Assert.IsType<UnauthorizedResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        #endregion

        // El controller arma algunas respuestas con objetos anonimos: se leen por nombre de propiedad.
        private static object? Propiedad(object? objeto, string nombre)
            => objeto?.GetType().GetProperty(nombre)?.GetValue(objeto);

        private OportunidadesController CrearControlador(int usuarioId) {
            ClaimsPrincipal usuario = new(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())],
                "Cookies"));

            return ArmarControlador(usuario);
        }

        private OportunidadesController CrearControladorSinSesion()
            => ArmarControlador(new ClaimsPrincipal(new ClaimsIdentity()));

        private OportunidadesController ArmarControlador(ClaimsPrincipal usuario) {
            return new OportunidadesController(_servicioMock.Object) {
                ControllerContext = new ControllerContext {
                    HttpContext = new DefaultHttpContext { User = usuario }
                }
            };
        }
    }
}