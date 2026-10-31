using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios
{
    public class TransporteServicioTests
    {
        private const int ConductorId = 5;
        private const int OtroConductorId = 99;

        // 14:00 UTC son las 11:00 en Argentina: el servicio tiene que guardar
        // la hora local, no la del servidor.
        private static readonly DateTimeOffset AhoraUtc = new(2026, 11, 1, 14, 0, 0, TimeSpan.Zero);
        private static readonly DateTime AhoraArgentina = new(2026, 11, 1, 11, 0, 0);

        private readonly Mock<ITransporteRepositorio> _repositorioMock;
        private readonly TransporteServicio _servicio;

        public TransporteServicioTests()
        {
            _repositorioMock = new Mock<ITransporteRepositorio>();
            _servicio = new TransporteServicio(_repositorioMock.Object, new RelojFijo(AhoraUtc));
        }

        // ---------- Listado ----------

        [Fact]
        public async Task ListarAsync_DevuelveLosDatosQueMuestraLaPantalla()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1,
                conductorId: ConductorId,
                viajeId: 10,
                envioId: 100,
                descripcion: "Caja de libros",
                pesoKg: 3.5,
                tamanio: TamanioEnvio.Mediano,
                monto: 4500m,
                origen: "Liniers",
                destino: "Once",
                remitenteNombre: "Carla",
                remitenteApellido: "Méndez");

            _repositorioMock
                .Setup(r => r.ListarPorConductorAsync(ConductorId, null))
                .ReturnsAsync([transporte]);

            List<TransporteListadoDTO> resultado = await _servicio.ListarAsync(ConductorId, null);

            TransporteListadoDTO dto = Assert.Single(resultado);
            Assert.Equal(1, dto.AsignacionId);
            Assert.Equal(10, dto.ViajeId);
            Assert.Equal(100, dto.EnvioId);
            Assert.Equal("Caja de libros", dto.Descripcion);
            Assert.Equal(3.5, dto.PesoKg);
            Assert.Equal(TamanioEnvio.Mediano, dto.Tamanio);
            Assert.Equal("Liniers", dto.Origen);
            Assert.Equal("Once", dto.Destino);
            Assert.Equal(4500m, dto.Ganancia);
            Assert.Equal(EstadoEnvio.ConductorAsignado, dto.Estado);
            Assert.Equal("Carla Méndez", dto.RemitenteNombre);
            Assert.Equal(TransporteTestData.FechaSalida, dto.FechaSalidaViaje);
        }

        [Fact]
        public async Task ListarAsync_CuandoHayFiltroDeEstado_SeLoPasaAlRepositorio()
        {
            _repositorioMock
                .Setup(r => r.ListarPorConductorAsync(ConductorId, EstadoEnvio.Retirado))
                .ReturnsAsync([]);

            await _servicio.ListarAsync(ConductorId, EstadoEnvio.Retirado);

            _repositorioMock.Verify(
                r => r.ListarPorConductorAsync(ConductorId, EstadoEnvio.Retirado), Times.Once);
        }

        [Theory]
        [InlineData(EstadoEnvio.ConductorAsignado, EstadoEnvio.Retirado)]
        [InlineData(EstadoEnvio.Retirado, EstadoEnvio.EnCamino)]
        [InlineData(EstadoEnvio.EnCamino, EstadoEnvio.Entregado)]
        public async Task ListarAsync_IndicaCualEsElProximoPasoDelTransporte(
            EstadoEnvio estadoActual, EstadoEnvio proximoEsperado)
        {
            _repositorioMock
                .Setup(r => r.ListarPorConductorAsync(ConductorId, null))
                .ReturnsAsync([TransporteTestData.CrearTransporte(estadoEnvio: estadoActual)]);

            List<TransporteListadoDTO> resultado = await _servicio.ListarAsync(ConductorId, null);

            Assert.Equal(proximoEsperado, Assert.Single(resultado).ProximoEstado);
        }

        [Theory]
        [InlineData(EstadoEnvio.Entregado)]
        [InlineData(EstadoEnvio.Cancelado)]
        public async Task ListarAsync_CuandoElTransporteYaTermino_NoHayProximoPaso(EstadoEnvio estadoActual)
        {
            _repositorioMock
                .Setup(r => r.ListarPorConductorAsync(ConductorId, null))
                .ReturnsAsync([TransporteTestData.CrearTransporte(estadoEnvio: estadoActual)]);

            List<TransporteListadoDTO> resultado = await _servicio.ListarAsync(ConductorId, null);

            Assert.Null(Assert.Single(resultado).ProximoEstado);
        }

        // ---------- Detalle ----------

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ObtenerDetalleAsync_CuandoNoExisteOElViajeEsDeOtroConductor_DevuelveNull(
            bool esDeOtroConductor)
        {
            Asignacion? transporte = esDeOtroConductor
                ? TransporteTestData.CrearTransporte(id: 1, conductorId: OtroConductorId)
                : null;

            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(1)).ReturnsAsync(transporte);

            TransporteDetalleDTO? resultado = await _servicio.ObtenerDetalleAsync(1, ConductorId);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoLaAsignacionFueCancelada_DevuelveNull()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1, conductorId: ConductorId, estadoAsignacion: EstadoViaje.CANCELADO);

            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(1)).ReturnsAsync(transporte);

            TransporteDetalleDTO? resultado = await _servicio.ObtenerDetalleAsync(1, ConductorId);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoEsDelConductor_DevuelveElDetalleCompleto()
        {
            DateTime aceptacion = new(2026, 10, 30, 12, 0, 0);
            DateTime retiro = new(2026, 11, 1, 9, 0, 0);

            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1,
                conductorId: ConductorId,
                estadoEnvio: EstadoEnvio.Retirado,
                fechaAceptacion: aceptacion,
                fechaRetiro: retiro);

            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(1)).ReturnsAsync(transporte);

            TransporteDetalleDTO? resultado = await _servicio.ObtenerDetalleAsync(1, ConductorId);

            Assert.NotNull(resultado);
            Assert.Equal("remitente7@test.com", resultado!.RemitenteEmail);
            Assert.Equal("Ramos Mejía", resultado.ViajeOrigen);
            Assert.Equal("Microcentro", resultado.ViajeDestino);
            Assert.Equal(aceptacion, resultado.FechaAceptacion);
            Assert.Equal(retiro, resultado.FechaRetiro);
            Assert.Null(resultado.FechaEntrega);
            Assert.Equal(EstadoEnvio.EnCamino, resultado.ProximoEstado);
        }

        // ---------- Avanzar: pertenencia ----------

        [Fact]
        public async Task AvanzarAsync_CuandoElTransporteNoExiste_DevuelveNoEncontrado()
        {
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync((Asignacion?)null);

            ResultadoOperacionTransporte resultado =
                await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.Retirado);

            Assert.True(resultado.NoEncontrado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task AvanzarAsync_CuandoElViajeEsDeOtroConductor_DevuelveNoEncontrado()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(id: 1, conductorId: OtroConductorId);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            ResultadoOperacionTransporte resultado =
                await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.Retirado);

            Assert.True(resultado.NoEncontrado);
            Assert.Equal(EstadoEnvio.ConductorAsignado, transporte.Envio.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        // ---------- Avanzar: transiciones validas ----------

        [Theory]
        [InlineData(EstadoEnvio.ConductorAsignado, EstadoEnvio.Retirado)]
        [InlineData(EstadoEnvio.Retirado, EstadoEnvio.EnCamino)]
        [InlineData(EstadoEnvio.EnCamino, EstadoEnvio.Entregado)]
        public async Task AvanzarAsync_CuandoLaTransicionEsValida_ActualizaElEstadoDelEnvio(
            EstadoEnvio estadoActual, EstadoEnvio destino)
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1, conductorId: ConductorId, estadoEnvio: estadoActual);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            ResultadoOperacionTransporte resultado =
                await _servicio.AvanzarAsync(1, ConductorId, destino);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(destino, transporte.Envio.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task AvanzarAsync_CuandoMarcaRetirado_GuardaLaFechaDeRetiro()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1, conductorId: ConductorId, estadoEnvio: EstadoEnvio.ConductorAsignado);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.Retirado);

            Assert.Equal(AhoraArgentina, transporte.FechaRetiro);
            Assert.Null(transporte.FechaEntrega);
        }

        [Fact]
        public async Task AvanzarAsync_CuandoMarcaEntregado_GuardaLaFechaDeEntrega()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1,
                conductorId: ConductorId,
                estadoEnvio: EstadoEnvio.EnCamino,
                fechaRetiro: new DateTime(2026, 11, 1, 9, 0, 0));
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.Entregado);

            Assert.Equal(AhoraArgentina, transporte.FechaEntrega);
        }

        [Fact]
        public async Task AvanzarAsync_CuandoMarcaEnCamino_NoTocaLasFechas()
        {
            DateTime retiro = new(2026, 11, 1, 9, 0, 0);
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1, conductorId: ConductorId, estadoEnvio: EstadoEnvio.Retirado, fechaRetiro: retiro);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.EnCamino);

            Assert.Equal(retiro, transporte.FechaRetiro);
            Assert.Null(transporte.FechaEntrega);
        }

        [Fact]
        public async Task AvanzarAsync_CuandoSaleBien_DevuelveElTransporteYaActualizado()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1, conductorId: ConductorId, estadoEnvio: EstadoEnvio.Retirado);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            ResultadoOperacionTransporte resultado =
                await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.EnCamino);

            Assert.NotNull(resultado.Transporte);
            Assert.Equal(EstadoEnvio.EnCamino, resultado.Transporte!.Estado);
            Assert.Equal(EstadoEnvio.Entregado, resultado.Transporte.ProximoEstado);
        }

        // ---------- Avanzar: transiciones invalidas ----------

        [Fact]
        public async Task AvanzarAsync_NoMarcaComoEntregadoUnPaqueteQueNoFueRetirado()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1, conductorId: ConductorId, estadoEnvio: EstadoEnvio.ConductorAsignado);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            ResultadoOperacionTransporte resultado =
                await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.Entregado);

            Assert.False(resultado.EsExitoso);
            Assert.False(resultado.NoEncontrado);
            Assert.Equal(
                "No se puede marcar como entregado un paquete que todavía no fue retirado.",
                resultado.Conflicto);
            Assert.Equal(EstadoEnvio.ConductorAsignado, transporte.Envio.Estado);
            Assert.Null(transporte.FechaEntrega);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Theory]
        // Saltear pasos
        [InlineData(EstadoEnvio.ConductorAsignado, EstadoEnvio.EnCamino)]
        [InlineData(EstadoEnvio.Retirado, EstadoEnvio.Entregado)]
        // Repetir el paso que ya se dio
        [InlineData(EstadoEnvio.Retirado, EstadoEnvio.Retirado)]
        [InlineData(EstadoEnvio.Entregado, EstadoEnvio.Entregado)]
        // Volver atras
        [InlineData(EstadoEnvio.EnCamino, EstadoEnvio.Retirado)]
        [InlineData(EstadoEnvio.Entregado, EstadoEnvio.EnCamino)]
        // Estados que no son pasos del transporte
        [InlineData(EstadoEnvio.ConductorAsignado, EstadoEnvio.BuscandoConductor)]
        [InlineData(EstadoEnvio.Retirado, EstadoEnvio.Cancelado)]
        public async Task AvanzarAsync_CuandoLaTransicionNoCorresponde_DevuelveConflictoYNoCambiaNada(
            EstadoEnvio estadoActual, EstadoEnvio destino)
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1, conductorId: ConductorId, estadoEnvio: estadoActual);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            ResultadoOperacionTransporte resultado =
                await _servicio.AvanzarAsync(1, ConductorId, destino);

            Assert.False(resultado.EsExitoso);
            Assert.False(resultado.NoEncontrado);
            Assert.False(string.IsNullOrWhiteSpace(resultado.Conflicto));
            Assert.Equal(estadoActual, transporte.Envio.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task AvanzarAsync_CuandoElEnvioEstaCancelado_DevuelveConflicto()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1, conductorId: ConductorId, estadoEnvio: EstadoEnvio.Cancelado);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            ResultadoOperacionTransporte resultado =
                await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.Retirado);

            Assert.False(resultado.EsExitoso);
            Assert.Equal(
                "El envío fue cancelado: ya no se puede avanzar el transporte.",
                resultado.Conflicto);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task AvanzarAsync_CuandoLaAsignacionFueCancelada_DevuelveConflicto()
        {
            Asignacion transporte = TransporteTestData.CrearTransporte(
                id: 1,
                conductorId: ConductorId,
                estadoEnvio: EstadoEnvio.ConductorAsignado,
                estadoAsignacion: EstadoViaje.CANCELADO);
            _repositorioMock.Setup(r => r.ObtenerParaModificarAsync(1)).ReturnsAsync(transporte);

            ResultadoOperacionTransporte resultado =
                await _servicio.AvanzarAsync(1, ConductorId, EstadoEnvio.Retirado);

            Assert.False(resultado.EsExitoso);
            Assert.False(resultado.NoEncontrado);
            Assert.Equal(EstadoEnvio.ConductorAsignado, transporte.Envio.Estado);
            _repositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }
    }
}
