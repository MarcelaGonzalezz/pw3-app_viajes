using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Repositorios
{
    public class TransporteRepositorioTests
    {
        private const int ConductorId = 5;

        [Fact]
        public async Task ListarPorConductorAsync_CuandoHayTransportesDeVariosConductores_FiltraPorConductor()
        {
            var db = new DbContextMockFactory(asignacionesIniciales:
            [
                TransporteTestData.CrearTransporte(id: 1, conductorId: ConductorId, viajeId: 10),
                TransporteTestData.CrearTransporte(id: 2, conductorId: 99, viajeId: 11),
                TransporteTestData.CrearTransporte(id: 3, conductorId: ConductorId, viajeId: 12)
            ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorConductorAsync(ConductorId, null);

            Assert.Equal(new[] { 1, 3 }, resultado.Select(a => a.Id).ToArray());
        }

        [Fact]
        public async Task ListarPorConductorAsync_NoDevuelveLasAsignacionesCanceladas()
        {
            var db = new DbContextMockFactory(asignacionesIniciales:
            [
                TransporteTestData.CrearTransporte(id: 1, conductorId: ConductorId),
                TransporteTestData.CrearTransporte(
                    id: 2, conductorId: ConductorId, estadoAsignacion: EstadoViaje.CANCELADO)
            ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorConductorAsync(ConductorId, null);

            Assert.Equal(1, Assert.Single(resultado).Id);
        }

        [Fact]
        public async Task ListarPorConductorAsync_CuandoHayFiltro_FiltraPorElEstadoDelEnvio()
        {
            var db = new DbContextMockFactory(asignacionesIniciales:
            [
                TransporteTestData.CrearTransporte(
                    id: 1, conductorId: ConductorId, estadoEnvio: EstadoEnvio.ConductorAsignado),
                TransporteTestData.CrearTransporte(
                    id: 2, conductorId: ConductorId, estadoEnvio: EstadoEnvio.Retirado),
                TransporteTestData.CrearTransporte(
                    id: 3, conductorId: ConductorId, estadoEnvio: EstadoEnvio.Entregado)
            ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorConductorAsync(ConductorId, EstadoEnvio.Retirado);

            Assert.Equal(2, Assert.Single(resultado).Id);
        }

        [Fact]
        public async Task ListarPorConductorAsync_OrdenaPorFechaDeSalidaDelViaje()
        {
            DateTime baseFecha = new(2026, 11, 1, 8, 0, 0);
            var db = new DbContextMockFactory(asignacionesIniciales:
            [
                TransporteTestData.CrearTransporte(
                    id: 1, conductorId: ConductorId, viajeId: 10, fechaSalida: baseFecha.AddDays(3)),
                TransporteTestData.CrearTransporte(
                    id: 2, conductorId: ConductorId, viajeId: 11, fechaSalida: baseFecha),
                TransporteTestData.CrearTransporte(
                    id: 3, conductorId: ConductorId, viajeId: 12, fechaSalida: baseFecha.AddDays(1))
            ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorConductorAsync(ConductorId, null);

            Assert.Equal(new[] { 2, 3, 1 }, resultado.Select(a => a.Id).ToArray());
        }

        [Fact]
        public async Task ListarPorConductorAsync_CuandoElConductorNoTieneTransportes_DevuelveListaVacia()
        {
            var db = new DbContextMockFactory(asignacionesIniciales:
            [
                TransporteTestData.CrearTransporte(id: 1, conductorId: 99)
            ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorConductorAsync(ConductorId, null);

            Assert.Empty(resultado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoExiste_DevuelveLaAsignacionConSusRelaciones()
        {
            var transporte = TransporteTestData.CrearTransporte(id: 7, conductorId: ConductorId);
            var db = new DbContextMockFactory(asignacionesIniciales: [transporte]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerDetalleAsync(7);

            Assert.NotNull(resultado);
            Assert.Same(transporte, resultado);
            Assert.Equal(ConductorId, resultado!.Viaje.Ruta.UsuarioId);
            Assert.Equal("Liniers", resultado.Envio.DireccionRetiro.Texto);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoNoExiste_DevuelveNull()
        {
            var db = new DbContextMockFactory(asignacionesIniciales:
            [
                TransporteTestData.CrearTransporte(id: 7, conductorId: ConductorId)
            ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerDetalleAsync(99);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerParaModificarAsync_DevuelveLaAsignacionSolicitada()
        {
            var db = new DbContextMockFactory(asignacionesIniciales:
            [
                TransporteTestData.CrearTransporte(id: 1, conductorId: ConductorId),
                TransporteTestData.CrearTransporte(id: 2, conductorId: ConductorId)
            ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerParaModificarAsync(2);

            Assert.NotNull(resultado);
            Assert.Equal(2, resultado!.Id);
        }

        [Fact]
        public async Task GuardarCambiosAsync_LlamaASaveChanges()
        {
            var db = new DbContextMockFactory();
            var repositorio = CrearRepositorio(db);

            await repositorio.GuardarCambiosAsync();

            db.Contexto.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        private static TransporteRepositorio CrearRepositorio(DbContextMockFactory db)
            => new(db.Contexto.Object);
    }
}
