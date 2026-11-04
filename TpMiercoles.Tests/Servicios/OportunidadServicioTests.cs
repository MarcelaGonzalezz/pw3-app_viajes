using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Tests.Helpers;
using Xunit;

namespace TpMiercoles.Tests.Servicios
{
    public class OportunidadServicioTests
    {
        private const int ConductorId = 5;
        private const int ViajeId = 10;
        private const int EnvioId = 20;

        private readonly Mock<IViajeRepositorio> _viajeRepositorioMock;
        private readonly Mock<IEnvioRepositorio> _envioRepositorioMock;
        private readonly Mock<IServicioRutas> _servicioRutasMock;
        private readonly OportunidadServicio _servicio;

        public OportunidadServicioTests()
        {
            _viajeRepositorioMock = new Mock<IViajeRepositorio>();
            _envioRepositorioMock = new Mock<IEnvioRepositorio>();
            _servicioRutasMock = new Mock<IServicioRutas>();

            _servicio = new OportunidadServicio(
                _viajeRepositorioMock.Object,
                _envioRepositorioMock.Object,
                _servicioRutasMock.Object,
                TimeProvider.System);
        }

        [Fact]
        public async Task ListarAsync_CuandoLaRutaDelViajeEstaInactiva_AgregaAvisoDeRutaInactiva()
        {
            // Arrange
            var viaje = ViajeTestData.CrearViaje(
                id: ViajeId,
                usuarioId: ConductorId,
                fechaSalida: DateTime.Now.AddDays(2),
                estado: EstadoActividadViaje.ACTIVO);
            viaje.Ruta.Estado = EstadoRuta.INACTIVA;

            _viajeRepositorioMock
                .Setup(r => r.ObtenerDetalleAsync(ViajeId))
                .ReturnsAsync(viaje);

            // Act
            var resultado = await _servicio.ListarAsync(ViajeId, ConductorId);

            // Assert
            Assert.NotNull(resultado);
            Assert.Contains("La ruta está inactiva: reactivala para buscar oportunidades.", resultado!.AvisosDelViaje);
            _envioRepositorioMock.Verify(e => e.ListarDisponiblesAsync(It.IsAny<DateTime>(), It.IsAny<int>()), Times.Never);
            _servicioRutasMock.Verify(s => s.CalcularRutaAsync(It.IsAny<Coordenada>(), It.IsAny<Coordenada>(), It.IsAny<IReadOnlyList<Coordenada>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AceptarAsync_CuandoLaRutaDelViajeEstaInactiva_RetornaConflicto()
        {
            // Arrange
            var viaje = ViajeTestData.CrearViaje(
                id: ViajeId,
                usuarioId: ConductorId,
                fechaSalida: DateTime.Now.AddDays(2),
                estado: EstadoActividadViaje.ACTIVO);
            viaje.Ruta.Estado = EstadoRuta.INACTIVA;

            var envio = new Envio
            {
                Id = EnvioId,
                UsuarioId = 99,
                Estado = EstadoEnvio.BuscandoConductor,
                FechaDesde = viaje.FechaSalida.Date.AddDays(-1),
                FechaHasta = viaje.FechaSalida.Date.AddDays(1),
                Paquete = new Paquete { PesoKg = 5 }
            };

            _viajeRepositorioMock
                .Setup(r => r.ObtenerParaAsignarAsync(ViajeId))
                .ReturnsAsync(viaje);
            _envioRepositorioMock
                .Setup(e => e.ObtenerParaOportunidadAsync(EnvioId))
                .ReturnsAsync(envio);

            // Act
            var resultado = await _servicio.AceptarAsync(ViajeId, EnvioId, ConductorId);

            // Assert
            Assert.NotNull(resultado);
            Assert.False(resultado.EsExitoso);
            Assert.Contains("La ruta está inactiva: reactivala para buscar oportunidades.", resultado.Conflicto);
            _viajeRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }
    }
}
