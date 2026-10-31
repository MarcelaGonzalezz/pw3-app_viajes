using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Repositorios
{
    public class EnvioRepositorioTests
    {
        #region ListarPorUsuarioAsync

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoHayEnviosDeVariosUsuarios_FiltraPorUsuario()
        {
            var db = new DbContextMockFactory(
                enviosIniciales:
                [
                    EnvioTestData.CrearEnvio(id: 1, usuarioId: 5),
                    EnvioTestData.CrearEnvio(id: 2, usuarioId: 7),
                    EnvioTestData.CrearEnvio(id: 3, usuarioId: 5)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, null);

            Assert.Equal([3, 1], resultado.Select(envio => envio.Id));
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_OrdenaPorIdDescendente()
        {
            var db = new DbContextMockFactory(
                enviosIniciales:
                [
                    EnvioTestData.CrearEnvio(id: 1, usuarioId: 5),
                    EnvioTestData.CrearEnvio(id: 3, usuarioId: 5),
                    EnvioTestData.CrearEnvio(id: 2, usuarioId: 5)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, null);

            Assert.Equal([3, 2, 1], resultado.Select(envio => envio.Id));
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoElEstadoEsNull_DevuelveEnviosDeTodosLosEstados()
        {
            var db = new DbContextMockFactory(
                enviosIniciales:
                [
                    EnvioTestData.CrearEnvio(id: 1, usuarioId: 5, estado: EstadoEnvio.BuscandoConductor),
                    EnvioTestData.CrearEnvio(id: 2, usuarioId: 5, estado: EstadoEnvio.Entregado),
                    EnvioTestData.CrearEnvio(id: 3, usuarioId: 5, estado: EstadoEnvio.Cancelado)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, null);

            Assert.Equal([3, 2, 1], resultado.Select(envio => envio.Id));
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoSeIndicaEstado_DevuelveSoloLosEnviosEnEseEstado()
        {
            var db = new DbContextMockFactory(
                enviosIniciales:
                [
                    EnvioTestData.CrearEnvio(id: 1, usuarioId: 5, estado: EstadoEnvio.BuscandoConductor),
                    EnvioTestData.CrearEnvio(id: 2, usuarioId: 5, estado: EstadoEnvio.Cancelado),
                    EnvioTestData.CrearEnvio(id: 3, usuarioId: 5, estado: EstadoEnvio.Cancelado)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, EstadoEnvio.Cancelado);

            Assert.Equal([3, 2], resultado.Select(envio => envio.Id));
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoElEstadoDeOtroUsuarioCoincide_NoLoIncluye()
        {
            var db = new DbContextMockFactory(
                enviosIniciales:
                [
                    EnvioTestData.CrearEnvio(id: 1, usuarioId: 5, estado: EstadoEnvio.Cancelado),
                    EnvioTestData.CrearEnvio(id: 2, usuarioId: 7, estado: EstadoEnvio.Cancelado)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, EstadoEnvio.Cancelado);

            var envio = Assert.Single(resultado);
            Assert.Equal(1, envio.Id);
        }

        [Fact]
        public async Task ListarPorUsuarioAsync_CuandoElUsuarioNoTieneEnvios_DevuelveListaVacia()
        {
            var db = new DbContextMockFactory(
                enviosIniciales: [EnvioTestData.CrearEnvio(id: 1, usuarioId: 7)]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ListarPorUsuarioAsync(5, null);

            Assert.Empty(resultado);
        }

        #endregion

        #region ObtenerDetalleAsync

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElEnvioExiste_DevuelveElEnvio()
        {
            var envio = EnvioTestData.CrearEnvio(
                id: 10,
                usuarioId: 5,
                asignaciones: [EnvioTestData.CrearAsignacionConConductor(1, nombre: "Carlos")]);
            var db = new DbContextMockFactory(enviosIniciales: [envio]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerDetalleAsync(10);

            Assert.NotNull(resultado);
            Assert.Same(envio, resultado);
            Assert.Equal("Ramos Mejía", resultado!.DireccionRetiro.Texto);
            Assert.Equal("Ciudadela", resultado.DireccionEntrega.Texto);
            Assert.Single(resultado.Asignaciones);
            Assert.Equal("Carlos", resultado.Asignaciones[0].Viaje.Ruta.Usuario.Nombre);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElEnvioNoExiste_DevuelveNull()
        {
            var db = new DbContextMockFactory(
                enviosIniciales: [EnvioTestData.CrearEnvio(id: 10)]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerDetalleAsync(99);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoHayVariosEnvios_DevuelveElSolicitado()
        {
            var db = new DbContextMockFactory(
                enviosIniciales:
                [
                    EnvioTestData.CrearEnvio(id: 1),
                    EnvioTestData.CrearEnvio(id: 2),
                    EnvioTestData.CrearEnvio(id: 3)
                ]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerDetalleAsync(2);

            Assert.NotNull(resultado);
            Assert.Equal(2, resultado!.Id);
        }

        #endregion

        #region ObtenerParaModificarAsync

        [Fact]
        public async Task ObtenerParaModificarAsync_CuandoElEnvioExiste_DevuelveElEnvioConSusAsignaciones()
        {
            var envio = EnvioTestData.CrearEnvio(
                id: 10,
                asignaciones:
                [
                    EnvioTestData.CrearAsignacionConConductor(1),
                    EnvioTestData.CrearAsignacionConConductor(2)
                ]);
            var db = new DbContextMockFactory(enviosIniciales: [envio]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerParaModificarAsync(10);

            Assert.Same(envio, resultado);
            Assert.Equal(2, resultado!.Asignaciones.Count);
        }

        [Fact]
        public async Task ObtenerParaModificarAsync_CuandoElEnvioNoExiste_DevuelveNull()
        {
            var db = new DbContextMockFactory(
                enviosIniciales: [EnvioTestData.CrearEnvio(id: 10)]);
            var repositorio = CrearRepositorio(db);

            var resultado = await repositorio.ObtenerParaModificarAsync(99);

            Assert.Null(resultado);
        }

        #endregion

        #region CrearAsync / GuardarCambiosAsync

        [Fact]
        public async Task CrearAsync_AgregaElEnvioYGuardaLosCambiosUnaVez()
        {
            var envio = EnvioTestData.CrearEnvio(id: 0);
            var db = new DbContextMockFactory();
            var repositorio = CrearRepositorio(db);

            await repositorio.CrearAsync(envio);

            db.Envios.Verify(enviosDbSet => enviosDbSet.Add(envio), Times.Once);
            db.Contexto.Verify(
                c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
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

        #endregion

        private static EnvioRepositorio CrearRepositorio(DbContextMockFactory db)
            => new(db.Contexto.Object);
    }
}

