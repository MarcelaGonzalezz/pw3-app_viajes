using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Mapa;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Servicios.Viajes;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Viajes {
    public class MapaViajeServicioTests {
        private const int UsuarioId = 1;
        private const string GeometriaGuardada = "{\"type\":\"LineString\",\"coordinates\":[[1,1],[2,2]]}";
        private const string GeometriaCalculada = "{\"type\":\"LineString\",\"coordinates\":[[3,3],[4,4]]}";

        private readonly Mock<IViajeRepositorio> _repositorioMock = new();
        private readonly Mock<IServicioRutas> _rutasMock = new();
        private readonly MapaViajeServicio _servicio;

        public MapaViajeServicioTests() {
            _servicio = new MapaViajeServicio(_repositorioMock.Object, _rutasMock.Object);
        }

        #region Permisos

        [Fact]
        public async Task ObtenerAsync_CuandoElViajeNoExiste_DevuelveNull() {
            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(99)).ReturnsAsync((Viaje?)null);

            MapaViajeDTO? mapa = await _servicio.ObtenerAsync(99, UsuarioId);

            Assert.Null(mapa);
        }

        [Fact]
        public async Task ObtenerAsync_CuandoElViajeEsDeOtroUsuario_DevuelveNullSinConsultarRutas() {
            ConfigurarViaje(ViajeTestData.CrearViaje(id: 1, usuarioId: 999));

            MapaViajeDTO? mapa = await _servicio.ObtenerAsync(1, UsuarioId);

            Assert.Null(mapa);
            VerificarConsultasARutas(Times.Never());
        }

        #endregion

        #region Recorrido habitual

        [Fact]
        public async Task ObtenerAsync_CuandoLaRutaTieneGeometriaGuardada_LaReusaSinConsultarRutas() {
            Viaje viaje = ViajeTestData.CrearViaje(id: 1, usuarioId: UsuarioId);
            viaje.Ruta.GeometriaJson = GeometriaGuardada;
            viaje.Ruta.DistanciaMetros = 12345;
            ConfigurarViaje(viaje);

            MapaViajeDTO mapa = (await _servicio.ObtenerAsync(1, UsuarioId))!;

            Assert.Equal(GeometriaGuardada, mapa.GeometriaBase);
            Assert.Equal(12.3, mapa.DistanciaBaseKm);
            Assert.Null(mapa.GeometriaConEnvios);
            Assert.Null(mapa.DesvioKm);
            VerificarConsultasARutas(Times.Never());
        }

        [Fact]
        public async Task ObtenerAsync_CuandoLaRutaNoTieneGeometria_CalculaElRecorridoHabitual() {
            ConfigurarViaje(ViajeTestData.CrearViaje(id: 1, usuarioId: UsuarioId));
            ConfigurarRutas(new ResultadoRuta(10000, 900, GeometriaCalculada));

            MapaViajeDTO mapa = (await _servicio.ObtenerAsync(1, UsuarioId))!;

            Assert.Equal(GeometriaCalculada, mapa.GeometriaBase);
            Assert.Equal(10, mapa.DistanciaBaseKm);
            VerificarConsultasARutas(Times.Once());
        }

        [Fact]
        public async Task ObtenerAsync_SinPaquetes_DevuelveOrigenParadasYDestinoEnOrden() {
            Viaje viaje = ViajeTestData.CrearViaje(
                id: 1,
                usuarioId: UsuarioId,
                paradas:
                [
                    ViajeTestData.CrearParada(11, "Parada dos", orden: 2),
                    ViajeTestData.CrearParada(10, "Parada uno", orden: 1)
                ]);
            ConfigurarViaje(viaje);
            ConfigurarRutas(new ResultadoRuta(10000, 900, GeometriaCalculada));

            MapaViajeDTO mapa = (await _servicio.ObtenerAsync(1, UsuarioId))!;

            Assert.Equal(
                new[] { TipoPuntoMapa.Origen, TipoPuntoMapa.Parada, TipoPuntoMapa.Parada, TipoPuntoMapa.Destino },
                mapa.Puntos.Select(p => p.Tipo));
            Assert.Equal("Parada uno", mapa.Puntos[1].Texto);
            Assert.Equal("Parada dos", mapa.Puntos[2].Texto);
        }

        #endregion

        #region Recorrido con paquetes

        [Fact]
        public async Task ObtenerAsync_ConUnPaqueteAsignado_MarcaRetiroYEntregaYCalculaElDesvio() {
            Viaje viaje = ViajeTestData.CrearViaje(
                id: 1,
                usuarioId: UsuarioId,
                asignaciones: [ViajeTestData.CrearAsignacion(id: 50, envioId: 7, direccionRetiro: "Liniers", direccionEntrega: "Once")]);
            viaje.Ruta.GeometriaJson = GeometriaGuardada;
            viaje.Ruta.DistanciaMetros = 10000;
            ConfigurarViaje(viaje);
            ConfigurarRutas(new ResultadoRuta(13500, 1200, GeometriaCalculada));

            MapaViajeDTO mapa = (await _servicio.ObtenerAsync(1, UsuarioId))!;

            PuntoMapaDTO retiro = Assert.Single(mapa.Puntos, p => p.Tipo == TipoPuntoMapa.Retiro);
            PuntoMapaDTO entrega = Assert.Single(mapa.Puntos, p => p.Tipo == TipoPuntoMapa.Entrega);
            Assert.Equal(7, retiro.EnvioId);
            Assert.Equal("Liniers", retiro.Texto);
            Assert.Equal(7, entrega.EnvioId);
            Assert.Equal("Once", entrega.Texto);

            Assert.Equal(GeometriaGuardada, mapa.GeometriaBase);
            Assert.Equal(GeometriaCalculada, mapa.GeometriaConEnvios);
            Assert.Equal(13.5, mapa.DistanciaConEnviosKm);
            Assert.Equal(3.5, mapa.DesvioKm);
        }

        [Fact]
        public async Task ObtenerAsync_ConUnPaqueteAsignado_PideElRecorridoPasandoPorRetiroYEntrega() {
            Viaje viaje = ViajeTestData.CrearViaje(
                id: 1,
                usuarioId: UsuarioId,
                asignaciones: [ViajeTestData.CrearAsignacion(id: 50, envioId: 7)]);
            viaje.Ruta.GeometriaJson = GeometriaGuardada;
            viaje.Ruta.DistanciaMetros = 10000;
            ConfigurarViaje(viaje);
            ConfigurarRutas(new ResultadoRuta(13500, 1200, GeometriaCalculada));

            await _servicio.ObtenerAsync(1, UsuarioId);

            // Sin paradas propias: las dos intermedias son el retiro y la entrega.
            _rutasMock.Verify(r => r.CalcularRutaAsync(
                It.IsAny<Coordenada>(),
                It.IsAny<Coordenada>(),
                It.Is<IReadOnlyList<Coordenada>?>(paradas => paradas != null && paradas.Count == 2),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ObtenerAsync_CuandoLaAsignacionEstaCancelada_NoLaMuestra() {
            Viaje viaje = ViajeTestData.CrearViaje(
                id: 1,
                usuarioId: UsuarioId,
                asignaciones: [ViajeTestData.CrearAsignacion(id: 50, envioId: 7, estado: EstadoViaje.CANCELADO)]);
            viaje.Ruta.GeometriaJson = GeometriaGuardada;
            viaje.Ruta.DistanciaMetros = 10000;
            ConfigurarViaje(viaje);

            MapaViajeDTO mapa = (await _servicio.ObtenerAsync(1, UsuarioId))!;

            Assert.DoesNotContain(mapa.Puntos, p => p.Tipo is TipoPuntoMapa.Retiro or TipoPuntoMapa.Entrega);
            Assert.Null(mapa.GeometriaConEnvios);
            VerificarConsultasARutas(Times.Never());
        }

        #endregion

        #region Fallas del servicio de rutas

        [Fact]
        public async Task ObtenerAsync_CuandoElServicioDeRutasFalla_DevuelveLosPuntosSinLineas() {
            Viaje viaje = ViajeTestData.CrearViaje(
                id: 1,
                usuarioId: UsuarioId,
                asignaciones: [ViajeTestData.CrearAsignacion(id: 50, envioId: 7)]);
            ConfigurarViaje(viaje);
            _rutasMock
                .Setup(r => r.CalcularRutaAsync(
                    It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
                    It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RutaNoEncontradaException("ORS caído"));

            MapaViajeDTO mapa = (await _servicio.ObtenerAsync(1, UsuarioId))!;

            Assert.Equal(4, mapa.Puntos.Count);
            Assert.Null(mapa.GeometriaBase);
            Assert.Null(mapa.GeometriaConEnvios);
        }

        [Fact]
        public async Task ObtenerAsync_CuandoElRecorridoConPaquetesEsEstimado_SumaElDesvioEstimadoALaDistanciaReal() {
            Viaje viaje = ViajeTestData.CrearViaje(
                id: 1,
                usuarioId: UsuarioId,
                asignaciones: [ViajeTestData.CrearAsignacion(id: 50, envioId: 7)]);
            viaje.Ruta.GeometriaJson = GeometriaGuardada;
            viaje.Ruta.DistanciaMetros = 10000;
            ConfigurarViaje(viaje);

            // Sin trazado = estimado por el mock. Primero se pide el recorrido con paquetes y después el habitual.
            _rutasMock
                .SetupSequence(r => r.CalcularRutaAsync(
                    It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
                    It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultadoRuta(15000, 1500))
                .ReturnsAsync(new ResultadoRuta(12000, 1200));

            MapaViajeDTO mapa = (await _servicio.ObtenerAsync(1, UsuarioId))!;

            // Desvío estimado: 15 - 12 = 3 km (mock contra mock), sumado a los 10 km reales.
            Assert.True(mapa.DistanciasEstimadas);
            Assert.Equal(10, mapa.DistanciaBaseKm);
            Assert.Equal(13, mapa.DistanciaConEnviosKm);
            Assert.Equal(3, mapa.DesvioKm);
            Assert.Equal(GeometriaGuardada, mapa.GeometriaBase);
        }

        [Fact]
        public async Task ObtenerAsync_CuandoOrsResponde_LasDistanciasNoSonEstimadas() {
            Viaje viaje = ViajeTestData.CrearViaje(
                id: 1,
                usuarioId: UsuarioId,
                asignaciones: [ViajeTestData.CrearAsignacion(id: 50, envioId: 7)]);
            viaje.Ruta.GeometriaJson = GeometriaGuardada;
            viaje.Ruta.DistanciaMetros = 10000;
            ConfigurarViaje(viaje);
            ConfigurarRutas(new ResultadoRuta(13500, 1200, GeometriaCalculada));

            MapaViajeDTO mapa = (await _servicio.ObtenerAsync(1, UsuarioId))!;

            Assert.False(mapa.DistanciasEstimadas);
            Assert.Equal(10, mapa.DistanciaBaseKm);
        }
        #endregion

        #region Helpers

        private void ConfigurarViaje(Viaje viaje) {
            _repositorioMock.Setup(r => r.ObtenerDetalleAsync(viaje.Id)).ReturnsAsync(viaje);
        }

        private void ConfigurarRutas(ResultadoRuta resultado) {
            _rutasMock
                .Setup(r => r.CalcularRutaAsync(
                    It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
                    It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultado);
        }

        private void VerificarConsultasARutas(Times veces) {
            _rutasMock.Verify(r => r.CalcularRutaAsync(
                It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
                It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()), veces);
        }

        #endregion
    }
}