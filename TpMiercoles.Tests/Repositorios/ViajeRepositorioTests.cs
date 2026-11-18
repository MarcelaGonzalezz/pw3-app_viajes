using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Repositorios
{
    public class ViajeRepositorioTests
    {
        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElViajeExiste_DevuelveElViaje()
        {
            var viaje = ViajeTestData.CrearViaje(
                id: 10,
                usuarioId: 5,
                paradas: [ViajeTestData.CrearParada(1, "Ciudadela")],
                asignaciones: [ViajeTestData.CrearAsignacion(1, envioId: 100)]);
            var db = new DbContextMockFactory(viajesIniciales: [viaje]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerDetalleAsync(10);

            Assert.NotNull(resultado);
            Assert.Same(viaje, resultado);
            Assert.Equal("Ramos Mejía", resultado!.DireccionOrigen.Texto);
            Assert.Single(resultado.Paradas);
            Assert.Single(resultado.Asignaciones);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElViajeNoExiste_DevuelveNull()
        {
            var db = new DbContextMockFactory(
                viajesIniciales: [ViajeTestData.CrearViaje(id: 10, usuarioId: 5)]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerDetalleAsync(99);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoHayVariosViajes_DevuelveElSolicitado()
        {
            var db = new DbContextMockFactory(
                viajesIniciales:
                [
                    ViajeTestData.CrearViaje(id: 1, usuarioId: 5),
                    ViajeTestData.CrearViaje(id: 2, usuarioId: 5),
                    ViajeTestData.CrearViaje(id: 3, usuarioId: 5)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerDetalleAsync(2);

            Assert.NotNull(resultado);
            Assert.Equal(2, resultado!.Id);
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoHayViajesDeVariosUsuarios_FiltraPorUsuario()
        {
            var db = new DbContextMockFactory(
                viajesIniciales:
                [
                    ViajeTestData.CrearViaje(id: 1, usuarioId: 5),
                    ViajeTestData.CrearViaje(id: 2, usuarioId: 7),
                    ViajeTestData.CrearViaje(id: 3, usuarioId: 5)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, null);

            Assert.Equal([1, 3], resultado.Select(viaje => viaje.Id));
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoElEstadoEsNull_DevuelveActivosEInactivos()
        {
            var db = new DbContextMockFactory(
                viajesIniciales:
                [
                    ViajeTestData.CrearViaje(id: 1, usuarioId: 5, estado: EstadoActividadViaje.ACTIVO),
                    ViajeTestData.CrearViaje(id: 2, usuarioId: 5, estado: EstadoActividadViaje.INACTIVO)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, null);

            Assert.Equal([1, 2], resultado.Select(viaje => viaje.Id));
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoElEstadoEsActivo_DevuelveSoloActivos()
        {
            var db = new DbContextMockFactory(
                viajesIniciales:
                [
                    ViajeTestData.CrearViaje(id: 1, usuarioId: 5, estado: EstadoActividadViaje.ACTIVO),
                    ViajeTestData.CrearViaje(id: 2, usuarioId: 5, estado: EstadoActividadViaje.INACTIVO)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, EstadoActividadViaje.ACTIVO);

            var viaje = Assert.Single(resultado);
            Assert.Equal(1, viaje.Id);
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoElEstadoEsInactivo_DevuelveSoloInactivos()
        {
            var db = new DbContextMockFactory(
                viajesIniciales:
                [
                    ViajeTestData.CrearViaje(id: 1, usuarioId: 5, estado: EstadoActividadViaje.ACTIVO),
                    ViajeTestData.CrearViaje(id: 2, usuarioId: 5, estado: EstadoActividadViaje.INACTIVO)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, EstadoActividadViaje.INACTIVO);

            var viaje = Assert.Single(resultado);
            Assert.Equal(2, viaje.Id);
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_OrdenaPorFechaSalidaYIdAscendente()
        {
            DateTime fecha = new(2026, 11, 2, 8, 0, 0);

            var db = new DbContextMockFactory(
                viajesIniciales:
                [
                    ViajeTestData.CrearViaje(id: 3, usuarioId: 5, fechaSalida: fecha),
                    ViajeTestData.CrearViaje(id: 1, usuarioId: 5, fechaSalida: fecha.AddDays(-1)),
                    ViajeTestData.CrearViaje(id: 2, usuarioId: 5, fechaSalida: fecha),
                    ViajeTestData.CrearViaje(id: 9, usuarioId: 7, fechaSalida: fecha.AddDays(-2))
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, null);

            Assert.Equal([1, 2, 3], resultado.Select(viaje => viaje.Id));
        }

        [Fact]
        public async Task ObtenerParaModificarAsync_CuandoElViajeExiste_DevuelveElViaje()
        {
            var viaje = ViajeTestData.CrearViaje(id: 10, usuarioId: 5);
            var db = new DbContextMockFactory(viajesIniciales: [viaje]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerParaModificarAsync(10);

            Assert.Same(viaje, resultado);
        }

        [Fact]
        public async Task ObtenerParaModificarAsync_CuandoElViajeNoExiste_DevuelveNull()
        {
            var db = new DbContextMockFactory(
                viajesIniciales: [ViajeTestData.CrearViaje(id: 10, usuarioId: 5)]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerParaModificarAsync(99);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task GuardarCambiosAsync_LlamaSaveChangesUnaVez()
        {
            var db = new DbContextMockFactory();
            var repositorio = CrearRepositorio(db);

            await repositorio.GuardarCambiosAsync();

            db.Contexto.Verify(
                c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExisteAlgunoAsync_CuandoHayONoViajes_DevuelveLoEsperado(bool hayViajes)
        {
            IEnumerable<Viaje> viajes = hayViajes
                ? [ViajeTestData.CrearViaje(id: 1, usuarioId: 5)]
                : [];
            var db = new DbContextMockFactory(viajesIniciales: viajes);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ExisteAlgunoAsync();

            Assert.Equal(hayViajes, resultado);
        }

        [Fact]
        public async Task AgregarAsync_CuandoSeAgreganViajes_LosGuardaTodos()
        {
            IEnumerable<Viaje> viajes =
            [
                ViajeTestData.CrearViaje(id: 1, usuarioId: 5),
                ViajeTestData.CrearViaje(id: 2, usuarioId: 5)
            ];
            var db = new DbContextMockFactory();
            var repositorio = CrearRepositorio(db);

            await repositorio.AgregarAsync(viajes);

            db.Viajes.Verify(viajesDbSet => viajesDbSet.AddRange(viajes), Times.Once);
            db.Contexto.Verify(
                c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ObtenerPorRutaYFechaAsync_CuandoCoincideRutaYFecha_DevuelveElViaje()
        {
            DateTime fecha = new(2026, 11, 2, 8, 0, 0);
            var db = new DbContextMockFactory(
                viajesIniciales:
                [
                    ViajeTestData.CrearViaje(id: 1, usuarioId: 5, fechaSalida: fecha),
                    ViajeTestData.CrearViaje(id: 2, usuarioId: 5, fechaSalida: fecha),
                    ViajeTestData.CrearViaje(id: 3, usuarioId: 5, fechaSalida: fecha.AddDays(1))
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerPorRutaYFechaAsync(1, fecha);

            Assert.NotNull(resultado);
            Assert.Equal(1, resultado!.Id);
        }

        [Fact]
        public async Task ObtenerPorRutaYFechaAsync_CuandoNoCoincide_DevuelveNull()
        {
            DateTime fecha = new(2026, 11, 2, 8, 0, 0);
            var db = new DbContextMockFactory(
                viajesIniciales:
                [
                    ViajeTestData.CrearViaje(id: 1, usuarioId: 5, fechaSalida: fecha),
                    ViajeTestData.CrearViaje(id: 2, usuarioId: 5, fechaSalida: fecha)
                ]);
            var repositorio = CrearRepositorio(db);

            Assert.Null(await repositorio.ObtenerPorRutaYFechaAsync(1, fecha.AddDays(1)));
            Assert.Null(await repositorio.ObtenerPorRutaYFechaAsync(99, fecha));
        }

        [Fact]
        public async Task ObtenerParaAsignarPorRutaYFechaAsync_DevuelveElViajeConSusAsignaciones()
        {
            DateTime fecha = new(2026, 11, 2, 8, 0, 0);
            var viaje = ViajeTestData.CrearViaje(
                id: 1,
                usuarioId: 5,
                fechaSalida: fecha,
                asignaciones: [ViajeTestData.CrearAsignacion(1, envioId: 100)]);
            var db = new DbContextMockFactory(viajesIniciales: [viaje]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerParaAsignarPorRutaYFechaAsync(1, fecha);

            Assert.NotNull(resultado);
            Assert.Single(resultado!.Asignaciones);
            Assert.Equal(100, resultado.Asignaciones[0].EnvioId);
        }

        [Fact]
        public async Task AgregarSinGuardarAsync_AgregaElViajeSinGuardar()
        {
            var db = new DbContextMockFactory();
            var repositorio = CrearRepositorio(db);
            var viaje = ViajeTestData.CrearViaje(id: 1, usuarioId: 5);

            await repositorio.AgregarSinGuardarAsync(viaje);

            db.Viajes.Verify(v => v.AddAsync(viaje, It.IsAny<CancellationToken>()), Times.Once);
            db.Contexto.Verify(
                c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static ViajeRepositorio CrearRepositorio(DbContextMockFactory db)
            => new(db.Contexto.Object);
    }
}
