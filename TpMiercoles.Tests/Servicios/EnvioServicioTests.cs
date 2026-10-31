using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios
{
    public class EnvioServicioTests
    {
        private const int UsuarioId = 1;

        // 10/03/2026 12:00 hora Argentina (15:00 UTC)
        private static readonly DateTimeOffset AhoraUtc = new(2026, 3, 10, 15, 0, 0, TimeSpan.Zero);
        private static readonly DateTime Hoy = new(2026, 3, 10);

        private readonly Mock<IEnvioRepositorio> _repositorioMock;
        private readonly RelojFijo _reloj;
        private readonly EnvioServicio _servicio;

        public EnvioServicioTests()
        {
            _repositorioMock = new Mock<IEnvioRepositorio>();
            _reloj = new RelojFijo(AhoraUtc);
            _servicio = new EnvioServicio(
                _repositorioMock.Object,
                MapperFactory.Crear(),
                _reloj);
        }

        #region CrearAsync

        [Fact]
        public async Task CrearAsync_CuandoLaFechaEsAnteriorAHoy_DevuelveInvalidoYNoGuarda()
        {
            var dto = EnvioTestData.CrearEnvioDTO(Hoy.AddDays(-1));

            var resultado = await _servicio.CrearAsync(dto, UsuarioId);

            Assert.False(resultado.EsExitoso);
            Assert.Equal("La fecha no puede ser anterior a hoy.", resultado.Error);
            _repositorioMock.Verify(r => r.CrearAsync(It.IsAny<Envio>()), Times.Never);
        }

        [Fact]
        public async Task CrearAsync_CuandoLaFechaEsHoy_DevuelveExitosoYGuarda()
        {
            var dto = EnvioTestData.CrearEnvioDTO(Hoy);

            var resultado = await _servicio.CrearAsync(dto, UsuarioId);

            Assert.True(resultado.EsExitoso);
            _repositorioMock.Verify(r => r.CrearAsync(It.IsAny<Envio>()), Times.Once);
        }

        [Fact]
        public async Task CrearAsync_CuandoEnUtcYaEsElDiaSiguiente_UsaLaFechaDeArgentina()
        {
            // 11/03 01:30 UTC == 10/03 22:30 en Argentina: "hoy" sigue siendo el 10
            _reloj.Fijar(new DateTimeOffset(2026, 3, 11, 1, 30, 0, TimeSpan.Zero));
            Envio? guardado = null;
            _repositorioMock
                .Setup(r => r.CrearAsync(It.IsAny<Envio>()))
                .Callback<Envio>(envio => guardado = envio)
                .Returns(Task.CompletedTask);

            var resultado = await _servicio.CrearAsync(EnvioTestData.CrearEnvioDTO(Hoy), UsuarioId);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(Hoy, guardado!.FechaDesde);
        }

        [Fact]
        public async Task CrearAsync_CuandoEsValido_AsignaUsuarioFechaDesdeYEstadoInicial()
        {
            Envio? guardado = null;
            _repositorioMock
                .Setup(r => r.CrearAsync(It.IsAny<Envio>()))
                .Callback<Envio>(envio => guardado = envio)
                .Returns(Task.CompletedTask);

            await _servicio.CrearAsync(EnvioTestData.CrearEnvioDTO(Hoy.AddDays(5)), UsuarioId);

            Assert.NotNull(guardado);
            Assert.Equal(UsuarioId, guardado!.UsuarioId);
            Assert.Equal(Hoy, guardado.FechaDesde);
            Assert.Equal(EstadoEnvio.BuscandoConductor, guardado.Estado);
        }

        [Fact]
        public async Task CrearAsync_CuandoEsValido_MapeaDireccionesPaqueteMontoYFecha()
        {
            var dto = EnvioTestData.CrearEnvioDTO(Hoy.AddDays(5));
            Envio? guardado = null;
            _repositorioMock
                .Setup(r => r.CrearAsync(It.IsAny<Envio>()))
                .Callback<Envio>(envio => guardado = envio)
                .Returns(Task.CompletedTask);

            await _servicio.CrearAsync(dto, UsuarioId);

            Assert.NotNull(guardado);
            Assert.Equal(dto.Origen.Texto, guardado!.DireccionRetiro.Texto);
            Assert.Equal(dto.Origen.Latitud, guardado.DireccionRetiro.Latitud);
            Assert.Equal(dto.Destino.Texto, guardado.DireccionEntrega.Texto);
            Assert.Equal(dto.Destino.Longitud, guardado.DireccionEntrega.Longitud);
            Assert.Equal(dto.FechaHasta, guardado.FechaHasta);
            Assert.Equal(dto.Monto, guardado.Monto);
            Assert.Equal(dto.Paquete.PesoKg, guardado.Paquete.PesoKg);
            Assert.Equal(dto.Paquete.Tamanio, guardado.Paquete.Tamanio);
            Assert.Equal(dto.Paquete.Descripcion, guardado.Paquete.Descripcion);
        }

        #endregion

        #region ListarAsync

        [Fact]
        public async Task ListarAsync_MapeaLosEnviosAlListadoDTO()
        {
            var envio = EnvioTestData.CrearEnvio(id: 7, estado: EstadoEnvio.Cancelado, monto: 2000m);
            _repositorioMock
                .Setup(r => r.ListarPorUsuarioAsync(UsuarioId, null))
                .ReturnsAsync([envio]);

            var resultado = await _servicio.ListarAsync(UsuarioId, null);

            var item = Assert.Single(resultado);
            Assert.Equal(7, item.Id);
            Assert.Equal("Ramos Mejía", item.Origen);
            Assert.Equal("Ciudadela", item.Destino);
            Assert.Equal(envio.FechaDesde, item.FechaDesde);
            Assert.Equal(envio.FechaHasta, item.FechaHasta);
            Assert.Equal(2.5, item.PesoKg);
            Assert.Equal(TamanioEnvio.Mediano, item.Tamanio);
            Assert.Equal("Zapatillas", item.Descripcion);
            Assert.Equal(2000m, item.Monto);
            Assert.Equal(EstadoEnvio.Cancelado, item.Estado);
        }

        [Fact]
        public async Task ListarAsync_PasaElUsuarioYElEstadoAlRepositorio()
        {
            _repositorioMock
                .Setup(r => r.ListarPorUsuarioAsync(UsuarioId, EstadoEnvio.Entregado))
                .ReturnsAsync([]);

            await _servicio.ListarAsync(UsuarioId, EstadoEnvio.Entregado);

            _repositorioMock.Verify(
                r => r.ListarPorUsuarioAsync(UsuarioId, EstadoEnvio.Entregado),
                Times.Once);
        }

        [Fact]
        public async Task ListarAsync_ConservaElOrdenDelRepositorio()
        {
            _repositorioMock
                .Setup(r => r.ListarPorUsuarioAsync(UsuarioId, null))
                .ReturnsAsync(
                [
                    EnvioTestData.CrearEnvio(id: 3),
                    EnvioTestData.CrearEnvio(id: 2),
                    EnvioTestData.CrearEnvio(id: 1)
                ]);

            var resultado = await _servicio.ListarAsync(UsuarioId, null);

            Assert.Equal([3, 2, 1], resultado.Select(envio => envio.Id));
        }

        [Fact]
        public async Task ListarAsync_CuandoNoHayEnvios_DevuelveListaVacia()
        {
            _repositorioMock
                .Setup(r => r.ListarPorUsuarioAsync(It.IsAny<int>(), It.IsAny<EstadoEnvio?>()))
                .ReturnsAsync([]);

            var resultado = await _servicio.ListarAsync(UsuarioId, null);

            Assert.Empty(resultado);
        }

        #endregion

        #region ObtenerDetalleAsync

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElEnvioNoExiste_DevuelveNull()
        {
            _repositorioMock
                .Setup(r => r.ObtenerDetalleAsync(99))
                .ReturnsAsync((Envio?)null);

            var resultado = await _servicio.ObtenerDetalleAsync(99, UsuarioId);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElEnvioEsDeOtroUsuario_DevuelveNull()
        {
            _repositorioMock
                .Setup(r => r.ObtenerDetalleAsync(10))
                .ReturnsAsync(EnvioTestData.CrearEnvio(id: 10, usuarioId: 2));

            var resultado = await _servicio.ObtenerDetalleAsync(10, UsuarioId);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_MapeaLosDatosBasicosDelEnvio()
        {
            var envio = EnvioTestData.CrearEnvio(id: 10, usuarioId: UsuarioId, estado: EstadoEnvio.BuscandoConductor);
            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.ObtenerDetalleAsync(10, UsuarioId);

            Assert.NotNull(resultado);
            Assert.Equal(10, resultado!.Id);
            Assert.Equal("Ramos Mejía", resultado.Origen);
            Assert.Equal("Ciudadela", resultado.Destino);
            Assert.Equal(envio.FechaDesde, resultado.FechaDesde);
            Assert.Equal(envio.FechaHasta, resultado.FechaHasta);
            Assert.Equal(2.5, resultado.PesoKg);
            Assert.Equal(TamanioEnvio.Mediano, resultado.Tamanio);
            Assert.Equal("Zapatillas", resultado.Descripcion);
            Assert.Equal(1500m, resultado.Monto);
            Assert.Equal(EstadoEnvio.BuscandoConductor, resultado.Estado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoNoHayAsignaciones_NoTieneConductor()
        {
            _repositorioMock
                .Setup(r => r.ObtenerDetalleAsync(10))
                .ReturnsAsync(EnvioTestData.CrearEnvio(id: 10, usuarioId: UsuarioId));

            var resultado = await _servicio.ObtenerDetalleAsync(10, UsuarioId);

            Assert.False(resultado!.TieneConductorAsignado);
            Assert.Null(resultado.ViajeId);
            Assert.Null(resultado.ConductorNombre);
            Assert.Null(resultado.ConductorEmail);
            Assert.Null(resultado.FechaSalidaViaje);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoLaUnicaAsignacionEstaCancelada_NoMuestraDatosDelConductor()
        {
            var envio = EnvioTestData.CrearEnvio(
                id: 10,
                usuarioId: UsuarioId,
                asignaciones: [EnvioTestData.CrearAsignacionConConductor(1, estado: EstadoViaje.CANCELADO)]);
            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.ObtenerDetalleAsync(10, UsuarioId);

            Assert.False(resultado!.TieneConductorAsignado);
            Assert.Null(resultado.ViajeId);
            Assert.Null(resultado.ConductorNombre);
            Assert.Null(resultado.ConductorEmail);
        }

        [Theory]
        [InlineData(EstadoViaje.PENDIENTE)]
        [InlineData(EstadoViaje.CONFIRMADO)]
        public async Task ObtenerDetalleAsync_CuandoLaAsignacionNoEstaCancelada_CompletaConductorYFechas(EstadoViaje estado)
        {
            var asignacion = EnvioTestData.CrearAsignacionConConductor(
                1, estado: estado, nombre: "Carlos", apellido: "Gomez", email: "carlos@mail.com");
            var envio = EnvioTestData.CrearEnvio(id: 10, usuarioId: UsuarioId, asignaciones: [asignacion]);
            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.ObtenerDetalleAsync(10, UsuarioId);

            Assert.True(resultado!.TieneConductorAsignado);
            Assert.Equal(asignacion.ViajeId, resultado.ViajeId);
            Assert.Equal("Carlos Gomez", resultado.ConductorNombre);
            Assert.Equal("carlos@mail.com", resultado.ConductorEmail);
            Assert.Equal(asignacion.Viaje.FechaSalida, resultado.FechaSalidaViaje);
            Assert.Equal(asignacion.FechaAceptacion, resultado.FechaAceptacion);
            Assert.Equal(asignacion.FechaRetiro, resultado.FechaRetiro);
            Assert.Equal(asignacion.FechaEntrega, resultado.FechaEntrega);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoHayVariasAsignaciones_UsaLaActivaMasReciente()
        {
            var envio = EnvioTestData.CrearEnvio(
                id: 10,
                usuarioId: UsuarioId,
                asignaciones:
                [
                    EnvioTestData.CrearAsignacionConConductor(1, nombre: "Viejo", apellido: "Activo"),
                    EnvioTestData.CrearAsignacionConConductor(2, nombre: "Nuevo", apellido: "Activo"),
                    EnvioTestData.CrearAsignacionConConductor(
                        3, estado: EstadoViaje.CANCELADO, nombre: "Cancelado", apellido: "Reciente")
                ]);
            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.ObtenerDetalleAsync(10, UsuarioId);

            Assert.Equal("Nuevo Activo", resultado!.ConductorNombre);
            Assert.Equal(20, resultado.ViajeId);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoLaAsignacionNoTieneViaje_NoTieneConductor()
        {
            var envio = EnvioTestData.CrearEnvio(
                id: 10,
                usuarioId: UsuarioId,
                asignaciones: [EnvioTestData.CrearAsignacionConConductor(1, conViaje: false)]);
            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.ObtenerDetalleAsync(10, UsuarioId);

            Assert.False(resultado!.TieneConductorAsignado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElConductorNoTieneNombre_NombreQuedaNullPeroMantieneElEmail()
        {
            var envio = EnvioTestData.CrearEnvio(
                id: 10,
                usuarioId: UsuarioId,
                asignaciones:
                [
                    EnvioTestData.CrearAsignacionConConductor(
                        1, nombre: "", apellido: " ", email: "anonimo@mail.com")
                ]);
            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.ObtenerDetalleAsync(10, UsuarioId);

            Assert.True(resultado!.TieneConductorAsignado);
            Assert.Null(resultado.ConductorNombre);
            Assert.Equal("anonimo@mail.com", resultado.ConductorEmail);
        }

        #endregion

        #region CancelarAsync

        [Fact]
        public async Task CancelarAsync_CuandoElEnvioNoExiste_DevuelveNoEncontrado()
        {
            _repositorioMock
                .Setup(r => r.ObtenerParaModificarAsync(99))
                .ReturnsAsync((Envio?)null);

            var resultado = await _servicio.CancelarAsync(99, UsuarioId);

            Assert.True(resultado.NoEncontrado);
            Assert.False(resultado.EsExitoso);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelarAsync_CuandoElEnvioEsDeOtroUsuario_DevuelveNoEncontradoYNoModifica()
        {
            var envio = EnvioTestData.CrearEnvio(id: 10, usuarioId: 2);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.CancelarAsync(10, UsuarioId);

            Assert.True(resultado.NoEncontrado);
            Assert.Equal(EstadoEnvio.BuscandoConductor, envio.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Theory]
        [InlineData(EstadoEnvio.ConductorAsignado)]
        [InlineData(EstadoEnvio.Retirado)]
        [InlineData(EstadoEnvio.EnCamino)]
        [InlineData(EstadoEnvio.Entregado)]
        [InlineData(EstadoEnvio.Cancelado)]
        public async Task CancelarAsync_CuandoElEstadoNoEsBuscandoConductor_DevuelveInvalidoYNoModifica(EstadoEnvio estado)
        {
            var envio = EnvioTestData.CrearEnvio(id: 10, usuarioId: UsuarioId, estado: estado);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.CancelarAsync(10, UsuarioId);

            Assert.False(resultado.EsExitoso);
            Assert.False(resultado.NoEncontrado);
            Assert.Equal(
                "Solo se pueden cancelar envíos que se encuentren en búsqueda de conductor.",
                resultado.Error);
            Assert.Equal(estado, envio.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelarAsync_CuandoEstaBuscandoConductor_CambiaElEstadoGuardaYDevuelveExitoso()
        {
            var envio = EnvioTestData.CrearEnvio(id: 10, usuarioId: UsuarioId);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.CancelarAsync(10, UsuarioId);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(EstadoEnvio.Cancelado, envio.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelarAsync_CuandoHayAsignacionesPendientes_LasCancelaTodas()
        {
            var envio = EnvioTestData.CrearEnvio(
                id: 10,
                usuarioId: UsuarioId,
                asignaciones:
                [
                    EnvioTestData.CrearAsignacionConConductor(1, estado: EstadoViaje.PENDIENTE),
                    EnvioTestData.CrearAsignacionConConductor(2, estado: EstadoViaje.CANCELADO),
                    EnvioTestData.CrearAsignacionConConductor(3, estado: EstadoViaje.CONFIRMADO)
                ]);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(10)).ReturnsAsync(envio);

            await _servicio.CancelarAsync(10, UsuarioId);

            Assert.All(envio.Asignaciones, a => Assert.Equal(EstadoViaje.CANCELADO, a.Estado));
        }

        [Fact]
        public async Task CancelarAsync_CuandoNoHayAsignaciones_CancelaElEnvioIgualmente()
        {
            var envio = EnvioTestData.CrearEnvio(id: 10, usuarioId: UsuarioId, asignaciones: []);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(10)).ReturnsAsync(envio);

            var resultado = await _servicio.CancelarAsync(10, UsuarioId);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(EstadoEnvio.Cancelado, envio.Estado);
        }

        #endregion
    }
}
