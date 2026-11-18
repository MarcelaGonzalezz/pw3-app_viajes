using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using TpMiercoles.Api.Controllers;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.Viajes;

namespace TpMiercoles.Tests.Api {
    // Verifica que cada endpoint de /api/viajes conteste el codigo HTTP que corresponde:
    // 200/204 cuando sale bien, 400 con los errores si los datos no sirven,
    // 404 si no existe o es de otro usuario y 401 sin sesion.
    public class ViajesApiControllerTests {
        private const int UsuarioId = 5;

        private readonly Mock<IViajeServicio> _servicioMock = new();

        #region GET /api/viajes

        [Fact]
        public async Task Listar_ConSesion_Devuelve200ConLosViajes() {
            List<ViajeListadoDTO> viajes = [new() { Id = 1 }, new() { Id = 2 }];
            _servicioMock
                .Setup(s => s.ListarAsync(UsuarioId, null, It.IsAny<DateTime>()))
                .ReturnsAsync(viajes);

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Listar(null);

            var ok = Assert.IsType<OkObjectResult>(resultado.Result);
            Assert.Same(viajes, ok.Value);
        }

        [Theory]
        [InlineData(EstadoActividadViaje.ACTIVO)]
        [InlineData(EstadoActividadViaje.INACTIVO)]
        public async Task Listar_ConFiltroDeEstado_SeLoPasaAlServicio(EstadoActividadViaje estado) {
            _servicioMock
                .Setup(s => s.ListarAsync(UsuarioId, estado, It.IsAny<DateTime>()))
                .ReturnsAsync([]);

            var controlador = CrearControlador(UsuarioId);

            await controlador.Listar(estado);

            _servicioMock.Verify(s => s.ListarAsync(UsuarioId, estado, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task Listar_SinSesion_Devuelve401SinConsultarElServicio() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Listar(null);

            Assert.IsType<UnauthorizedResult>(resultado.Result);
            _servicioMock.VerifyNoOtherCalls();
        }

        #endregion

        #region GET /api/viajes/{id}

        [Fact]
        public async Task Obtener_CuandoExisteYEsDelUsuario_Devuelve200() {
            ViajeDetalleDTO detalle = new() { Id = 1 };
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(1, UsuarioId, It.IsAny<DateTime>()))
                .ReturnsAsync(detalle);

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Obtener(1);

            var ok = Assert.IsType<OkObjectResult>(resultado.Result);
            Assert.Same(detalle, ok.Value);
        }

        [Fact]
        public async Task Obtener_CuandoNoExisteOEsDeOtroUsuario_Devuelve404() {
            _servicioMock
                .Setup(s => s.ObtenerDetalleAsync(1, UsuarioId, It.IsAny<DateTime>()))
                .ReturnsAsync((ViajeDetalleDTO?)null);

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Obtener(1);

            Assert.IsType<NotFoundResult>(resultado.Result);
        }

        [Fact]
        public async Task Obtener_SinSesion_Devuelve401() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Obtener(1);

            Assert.IsType<UnauthorizedResult>(resultado.Result);
            _servicioMock.VerifyNoOtherCalls();
        }

        #endregion

        #region PUT /api/viajes/{id}

        [Fact]
        public async Task Actualizar_ConDatosValidos_Devuelve204() {
            ViajeEdicionDTO datos = new() { Id = 1 };
            _servicioMock
                .Setup(s => s.ActualizarAsync(1, UsuarioId, datos))
                .ReturnsAsync(ResultadoOperacionViaje.Exitoso());

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Actualizar(1, datos);

            Assert.IsType<NoContentResult>(resultado);
        }

        [Fact]
        public async Task Actualizar_ConDatosInvalidos_Devuelve400ConLosErrores() {
            ViajeEdicionDTO datos = new() { Id = 1 };
            _servicioMock
                .Setup(s => s.ActualizarAsync(1, UsuarioId, datos))
                .ReturnsAsync(ResultadoOperacionViaje.Invalido(["La llegada debe ser posterior a la salida."]));

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Actualizar(1, datos);

            var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
            var errores = Assert.IsType<List<string>>(Propiedad(badRequest.Value, "errores"));
            Assert.Contains("La llegada debe ser posterior a la salida.", errores);
        }

        [Fact]
        public async Task Actualizar_CuandoNoExisteOEsDeOtroUsuario_Devuelve404() {
            ViajeEdicionDTO datos = new() { Id = 1 };
            _servicioMock
                .Setup(s => s.ActualizarAsync(1, UsuarioId, datos))
                .ReturnsAsync(ResultadoOperacionViaje.NoExiste());

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Actualizar(1, datos);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Actualizar_SinSesion_Devuelve401SinModificarNada() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Actualizar(1, new ViajeEdicionDTO());

            Assert.IsType<UnauthorizedResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        #endregion

        #region POST /api/viajes/{id}/desactivar

        [Fact]
        public async Task Desactivar_CuandoExiste_Devuelve200ConEstadoInactivoYPaquetesAsignados() {
            _servicioMock
                .Setup(s => s.DesactivarAsync(1, UsuarioId))
                .ReturnsAsync(ResultadoOperacionViaje.Exitoso(paquetesAfectados: 3));

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Desactivar(1);

            var ok = Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("INACTIVO", Propiedad(ok.Value, "estado"));
            Assert.Equal(3, Propiedad(ok.Value, "paquetesAsignados"));
        }

        [Fact]
        public async Task Desactivar_CuandoNoExisteOEsDeOtroUsuario_Devuelve404() {
            _servicioMock
                .Setup(s => s.DesactivarAsync(1, UsuarioId))
                .ReturnsAsync(ResultadoOperacionViaje.NoExiste());

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Desactivar(1);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Desactivar_SinSesion_Devuelve401SinModificarNada() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Desactivar(1);

            Assert.IsType<UnauthorizedResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        #endregion

        #region POST /api/viajes/{id}/activar

        [Fact]
        public async Task Activar_CuandoSePuede_Devuelve200ConEstadoActivo() {
            _servicioMock
                .Setup(s => s.ActivarAsync(1, UsuarioId, It.IsAny<DateTime>()))
                .ReturnsAsync(ResultadoOperacionViaje.Exitoso(paquetesAfectados: 2));

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Activar(1);

            var ok = Assert.IsType<OkObjectResult>(resultado);
            Assert.Equal("ACTIVO", Propiedad(ok.Value, "estado"));
            Assert.Equal(2, Propiedad(ok.Value, "paquetesAsignados"));
        }

        [Fact]
        public async Task Activar_ViajeVencido_Devuelve400ConElMotivo() {
            _servicioMock
                .Setup(s => s.ActivarAsync(1, UsuarioId, It.IsAny<DateTime>()))
                .ReturnsAsync(ResultadoOperacionViaje.Invalido(["El viaje ya venció y no se puede reactivar."]));

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Activar(1);

            var badRequest = Assert.IsType<BadRequestObjectResult>(resultado);
            var errores = Assert.IsType<List<string>>(Propiedad(badRequest.Value, "errores"));
            Assert.Single(errores);
        }

        [Fact]
        public async Task Activar_CuandoNoExisteOEsDeOtroUsuario_Devuelve404() {
            _servicioMock
                .Setup(s => s.ActivarAsync(1, UsuarioId, It.IsAny<DateTime>()))
                .ReturnsAsync(ResultadoOperacionViaje.NoExiste());

            var controlador = CrearControlador(UsuarioId);

            var resultado = await controlador.Activar(1);

            Assert.IsType<NotFoundResult>(resultado);
        }

        [Fact]
        public async Task Activar_SinSesion_Devuelve401SinModificarNada() {
            var controlador = CrearControladorSinSesion();

            var resultado = await controlador.Activar(1);

            Assert.IsType<UnauthorizedResult>(resultado);
            _servicioMock.VerifyNoOtherCalls();
        }

        #endregion

        // El controller arma respuestas con objetos anonimos (new { estado, paquetesAsignados }):
        // se leen por nombre de propiedad.
        private static object? Propiedad(object? objeto, string nombre)
            => objeto?.GetType().GetProperty(nombre)?.GetValue(objeto);

        private ViajesController CrearControlador(int usuarioId) {
            ClaimsPrincipal usuario = new(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())],
                "Cookies"));

            return ArmarControlador(usuario);
        }

        private ViajesController CrearControladorSinSesion()
            => ArmarControlador(new ClaimsPrincipal(new ClaimsIdentity()));

        private ViajesController ArmarControlador(ClaimsPrincipal usuario) {
            return new ViajesController(_servicioMock.Object) {
                ControllerContext = new ControllerContext {
                    HttpContext = new DefaultHttpContext { User = usuario }
                }
            };
        }
    }
}