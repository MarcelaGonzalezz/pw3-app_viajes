using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios {
    public class RutaServicioTests {
        private const int UsuarioId = 1;

        private readonly Mock<IRutaRepositorio> _rutaRepositorioMock;
        private readonly Mock<IUsuarioRepositorio> _usuarioRepositorioMock;
        private readonly RutaServicio _servicio;
        private readonly Mock<IRecorridoServicio> _recorridoServicioMock;

        // Fecha fija para los metodos que reciben la hora actual. Las rutas de estos tests
        // no tienen fecha de fin, asi que no vencen.
        private static readonly DateTime Ahora = new(2026, 11, 20, 8, 0, 0);

        public RutaServicioTests() {
            _rutaRepositorioMock = new Mock<IRutaRepositorio>();
            _usuarioRepositorioMock = new Mock<IUsuarioRepositorio>();
            _recorridoServicioMock = new Mock<IRecorridoServicio>();
            _recorridoServicioMock
                .Setup(r => r.CalcularAsync(It.IsAny<RecorridoDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultadoRuta(15000, 1200));

            _servicio = new RutaServicio(
                _rutaRepositorioMock.Object,
                _usuarioRepositorioMock.Object,
                MapperFactory.Crear(),
                _recorridoServicioMock.Object);
        }

        #region Caso feliz

        [Fact]
        public async Task CrearRutaAsync_CuandoLaRutaEsValida_CreaLaRutaConElUsuarioYLosDatosMapeados() {
            var dto = CrearRutaValida();
            var usuario = CrearUsuario();
            Ruta? rutaGuardada = null;

            ConfigurarUsuario(usuario);
            _rutaRepositorioMock
                .Setup(r => r.CrearRutaAsync(It.IsAny<Ruta>()))
                .Callback<Ruta>(ruta => rutaGuardada = ruta)
                .Returns(Task.CompletedTask);

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            Assert.NotNull(rutaGuardada);
            Assert.Same(usuario, rutaGuardada!.Usuario);
            Assert.Equal(dto.PesoMax, rutaGuardada.PesoMax);
            Assert.Equal(dto.CapacidadMaxPaquetes, rutaGuardada.CapacidadMaxPaquetes);
            Assert.Equal(dto.FechaInicio, rutaGuardada.FechaInicio);
            Assert.Equal(dto.FechaFin, rutaGuardada.FechaFin);
            Assert.Equal(dto.HoraSalida, rutaGuardada.HoraSalida);
            Assert.Equal(dto.HoraLlegada, rutaGuardada.HoraLlegada);
            Assert.Equal(dto.RutaDias.Count, rutaGuardada.RutaDias.Count);
            Assert.Equal(dto.Paradas.Count, rutaGuardada.Paradas.Count);
            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(rutaGuardada), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLaRutaEsValida_BuscaElUsuarioPorElIdRecibido() {
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, CrearRutaValida());

            _usuarioRepositorioMock.Verify(r => r.BuscarPorIdAsync(UsuarioId), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLaRutaNoTieneParadas_CreaLaRuta() {
            var dto = CrearRutaValida();
            dto.Paradas = [];
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoNoHayFechaFin_CreaLaRuta() {
            var dto = CrearRutaValida();
            dto.FechaFin = null;
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLaFechaFinEsIgualALaFechaInicio_CreaLaRuta() {
            var dto = CrearRutaValida();
            dto.FechaFin = dto.FechaInicio;
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLosOrdenesDeParadasEstanDesordenadosPeroSonConsecutivos_CreaLaRuta() {
            var dto = CrearRutaValida();
            dto.Paradas =
            [
                CrearParada(-34.60500m, -58.38300m, 3),
                CrearParada(-34.60600m, -58.38400m, 1),
                CrearParada(-34.60700m, -58.38500m, 2)
            ];
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoElOrigenYElDestinoSoloDifierenEnLaLatitud_CreaLaRuta() {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m);
            dto.DireccionDestino = CrearDireccion(10.12346m, 20.12345m);
            dto.Paradas = [];
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoElOrigenYElDestinoSoloDifierenEnLaLongitud_CreaLaRuta() {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m);
            dto.DireccionDestino = CrearDireccion(10.12345m, 20.12346m);
            dto.Paradas = [];
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        #endregion

        #region Origen y destino

        [Fact]
        public async Task CrearRutaAsync_CuandoElOrigenYElDestinoTienenLasMismasCoordenadas_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m);
            dto.DireccionDestino = CrearDireccion(10.12345m, 20.12345m);
            dto.Paradas = [];

            await AssertValidacionFallaAsync(dto, "El origen y el destino deben ser diferentes.");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoElOrigenYElDestinoTienenLasMismasCoordenadasPeroDistintoTexto_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m, "Calle A 123");
            dto.DireccionDestino = CrearDireccion(10.12345m, 20.12345m, "Calle B 456");
            dto.Paradas = [];

            await AssertValidacionFallaAsync(dto, "El origen y el destino deben ser diferentes.");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLasCoordenadasDifierenSoloDesdeElSextoDecimal_LasConsideraIguales() {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.123451m, 20.123451m);
            dto.DireccionDestino = CrearDireccion(10.123454m, 20.123454m);
            dto.Paradas = [];

            await AssertValidacionFallaAsync(dto, "El origen y el destino deben ser diferentes.");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLasCoordenadasEstanEnElPuntoMedioDelRedondeo_UsaRedondeoAlParMasCercano() {
            // 10.123455 -> 10.12346 y 10.123465 -> 10.12346 (MidpointRounding.ToEven).
            // Con redondeo "away from zero" darían 10.12346 y 10.12347, y serían distintas.
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.123455m, 20.12345m);
            dto.DireccionDestino = CrearDireccion(10.123465m, 20.12345m);
            dto.Paradas = [];

            await AssertValidacionFallaAsync(dto, "El origen y el destino deben ser diferentes.");
        }

        #endregion

        #region Días

        [Fact]
        public async Task CrearRutaAsync_CuandoNoHayDiasSeleccionados_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.RutaDias = [];

            await AssertValidacionFallaAsync(dto, "Debe seleccionar al menos un día.");
        }

        #endregion

        #region Peso y capacidad

        [Theory]
        [InlineData(0)]
        [InlineData(-0.5)]
        [InlineData(-10)]
        public async Task CrearRutaAsync_CuandoElPesoMaximoNoEsMayorACero_LanzaValidacionRutaException(double pesoMax) {
            var dto = CrearRutaValida();
            dto.PesoMax = pesoMax;

            await AssertValidacionFallaAsync(dto, "Peso y capacidad deben ser mayores que cero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public async Task CrearRutaAsync_CuandoLaCapacidadMaximaNoEsMayorACero_LanzaValidacionRutaException(int capacidad) {
            var dto = CrearRutaValida();
            dto.CapacidadMaxPaquetes = capacidad;

            await AssertValidacionFallaAsync(dto, "Peso y capacidad deben ser mayores que cero.");
        }

        #endregion

        #region Horarios

        [Theory]
        [InlineData(10, 10)]
        [InlineData(11, 10)]
        public async Task CrearRutaAsync_CuandoLaHoraDeSalidaNoEsAnteriorALaDeLlegada_LanzaValidacionRutaException(
            int horaSalida,
            int horaLlegada) {
            var dto = CrearRutaValida();
            dto.HoraSalida = new TimeOnly(horaSalida, 0);
            dto.HoraLlegada = new TimeOnly(horaLlegada, 0);

            await AssertValidacionFallaAsync(dto, "La hora de salida debe ser anterior a la de llegada.");
        }

        #endregion

        #region Fechas

        [Fact]
        public async Task CrearRutaAsync_CuandoLaFechaFinEsAnteriorALaFechaInicio_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.FechaInicio = new DateTime(2026, 11, 10);
            dto.FechaFin = new DateTime(2026, 11, 9);

            await AssertValidacionFallaAsync(dto, "La fecha de fin no puede ser anterior a la de inicio.");
        }

        #endregion

        #region Paradas

        [Fact]
        public async Task CrearRutaAsync_CuandoUnaParadaTieneLasCoordenadasDelOrigen_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.Paradas =
            [
                CrearParada(dto.DireccionOrigen.Latitud, dto.DireccionOrigen.Longitud, 1)
            ];

            await AssertValidacionFallaAsync(dto, "La parada no puede ser igual a la direccion de origen o destino");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoUnaParadaTieneLasCoordenadasDelDestino_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.Paradas =
            [
                CrearParada(dto.DireccionDestino.Latitud, dto.DireccionDestino.Longitud, 1)
            ];

            await AssertValidacionFallaAsync(dto, "La parada no puede ser igual a la direccion de origen o destino");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoUnaParadaSoloDifiereDelOrigenDesdeElSextoDecimal_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m);
            dto.DireccionDestino = CrearDireccion(30.00000m, 40.00000m);
            dto.Paradas = [CrearParada(10.123451m, 20.123451m, 1)];

            await AssertValidacionFallaAsync(dto, "La parada no puede ser igual a la direccion de origen o destino");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoDosParadasTienenLasMismasCoordenadas_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.Paradas =
            [
                CrearParada(-34.60500m, -58.38300m, 1),
                CrearParada(-34.60500m, -58.38300m, 2)
            ];

            await AssertValidacionFallaAsync(dto, "No se pueden repetir paradas.");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoDosParadasSoloDifierenDesdeElSextoDecimal_LanzaValidacionRutaException() {
            var dto = CrearRutaValida();
            dto.Paradas =
            [
                CrearParada(-34.605001m, -58.383001m, 1),
                CrearParada(-34.605004m, -58.383004m, 2)
            ];

            await AssertValidacionFallaAsync(dto, "No se pueden repetir paradas.");
        }

        [Theory]
        [InlineData(new[] { 2, 3 })]
        [InlineData(new[] { 0, 1 })]
        [InlineData(new[] { 1, 3 })]
        [InlineData(new[] { 1, 1 })]
        [InlineData(new[] { -1, 1 })]
        public async Task CrearRutaAsync_CuandoElOrdenDeLasParadasNoEsConsecutivoDesdeUno_LanzaValidacionRutaException(
            int[] ordenes) {
            var dto = CrearRutaValida();
            dto.Paradas = ordenes
                .Select((orden, indice) => CrearParada(-34.60500m - (indice * 0.001m), -58.38300m, orden))
                .ToList();

            await AssertValidacionFallaAsync(dto, "El orden de las paradas debe ser consecutivo, empezando en 1.");
        }

        #endregion

        #region Orden de operaciones

        [Fact]
        public async Task CrearRutaAsync_CuandoLaValidacionFalla_NoBuscaAlUsuarioNiCreaLaRuta() {
            var dto = CrearRutaValida();
            dto.RutaDias = [];

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.CrearRutaAsync(UsuarioId, dto));

            _usuarioRepositorioMock.Verify(r => r.BuscarPorIdAsync(It.IsAny<int>()), Times.Never);
            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Never);
        }

        #endregion

        #region Detalle: completar recorrido faltante

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoLaRutaNoTieneGeometria_LaCalculaYLaGuarda() {
            var ruta = RutaTestData.CrearRuta(id: 7, usuarioId: UsuarioId);
            const string geometria = "{\"type\":\"LineString\",\"coordinates\":[[-58.5,-34.6],[-58.4,-34.7]]}";

            _rutaRepositorioMock.Setup(r => r.ObtenerPorIdAsync(7)).ReturnsAsync(ruta);
            _recorridoServicioMock
                .Setup(r => r.CalcularAsync(It.IsAny<RecorridoDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultadoRuta(20000, 1500, geometria));

            RutaDTO detalle = await _servicio.ObtenerDetalleAsync(7, UsuarioId, Ahora);

            Assert.Equal(geometria, detalle.GeometriaJson);
            Assert.Equal(geometria, ruta.GeometriaJson);
            Assert.Equal(20000, ruta.DistanciaMetros);
            Assert.Equal(1500, ruta.DuracionSegundos);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoLaRutaYaTieneGeometria_NoRecalcula() {
            var ruta = RutaTestData.CrearRuta(id: 8, usuarioId: UsuarioId);
            ruta.GeometriaJson = "{\"type\":\"LineString\",\"coordinates\":[]}";

            _rutaRepositorioMock.Setup(r => r.ObtenerPorIdAsync(8)).ReturnsAsync(ruta);

            await _servicio.ObtenerDetalleAsync(8, UsuarioId, Ahora);

            _recorridoServicioMock.Verify(
                r => r.CalcularAsync(It.IsAny<RecorridoDTO>(), It.IsAny<CancellationToken>()), Times.Never);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElServicioDeRutasFalla_DevuelveElDetalleSinGuardar() {
            var ruta = RutaTestData.CrearRuta(id: 9, usuarioId: UsuarioId);

            _rutaRepositorioMock.Setup(r => r.ObtenerPorIdAsync(9)).ReturnsAsync(ruta);
            _recorridoServicioMock
                .Setup(r => r.CalcularAsync(It.IsAny<RecorridoDTO>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RutaNoEncontradaException("ORS caído"));

            RutaDTO detalle = await _servicio.ObtenerDetalleAsync(9, UsuarioId, Ahora);

            Assert.Null(detalle.GeometriaJson);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ObtenerDetalleAsync_CuandoElServicioNoDevuelveGeometria_NoGuarda() {
            var ruta = RutaTestData.CrearRuta(id: 10, usuarioId: UsuarioId);

            _rutaRepositorioMock.Setup(r => r.ObtenerPorIdAsync(10)).ReturnsAsync(ruta);
            // El constructor configura el mock sin geometría, como el MockServicioRutas.

            await _servicio.ObtenerDetalleAsync(10, UsuarioId, Ahora);

            Assert.Null(ruta.GeometriaJson);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        #endregion

        #region Desactivar y Activar ruta

        [Fact]
        public async Task DesactivarRutaAsync_CuandoRutaExisteYEsDelUsuario_CambiaEstadoAInactivaYGuardaCambios() {
            const int rutaId = 15;
            var ruta = new Ruta {
                Id = rutaId,
                UsuarioId = UsuarioId,
                Estado = EstadoRuta.ACTIVA
            };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            await _servicio.DesactivarRutaAsync(rutaId, UsuarioId);

            Assert.Equal(EstadoRuta.INACTIVA, ruta.Estado);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task DesactivarRutaAsync_CuandoRutaNoExiste_LanzaRutaNoEncontradaException() {
            const int rutaId = 99;
            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync((Ruta?)null);

            await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => _servicio.DesactivarRutaAsync(rutaId, UsuarioId));

            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task DesactivarRutaAsync_CuandoRutaNoPerteneceAlUsuario_LanzaUnauthorizedAccessException() {
            const int rutaId = 15;
            const int otroUsuarioId = 999;
            var ruta = new Ruta {
                Id = rutaId,
                UsuarioId = otroUsuarioId,
                Estado = EstadoRuta.ACTIVA
            };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _servicio.DesactivarRutaAsync(rutaId, UsuarioId));

            Assert.Equal(EstadoRuta.ACTIVA, ruta.Estado);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ActivarRutaAsync_CuandoRutaExisteYEsDelUsuario_CambiaEstadoAActivaYGuardaCambios() {
            const int rutaId = 15;
            var ruta = new Ruta {
                Id = rutaId,
                UsuarioId = UsuarioId,
                Estado = EstadoRuta.INACTIVA
            };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            await _servicio.ActivarRutaAsync(rutaId, UsuarioId, Ahora);

            Assert.Equal(EstadoRuta.ACTIVA, ruta.Estado);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        #endregion

        #region Vencimiento

        private static DateTime Noviembre(int dia, int hora, int minuto = 0)
            => new(2026, 11, dia, hora, minuto, 0);

        [Fact]
        public async Task ActivarRutaAsync_CuandoLaRutaVencio_LanzaValidacionYNoLaActiva() {
            var ruta = RutaTestData.CrearRuta(
                id: 15,
                usuarioId: UsuarioId,
                estado: EstadoRuta.INACTIVA,
                fechaFin: new DateTime(2026, 11, 19),
                horaLlegada: new TimeOnly(9, 30));
            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(15))
                .ReturnsAsync(ruta);

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.ActivarRutaAsync(15, UsuarioId, Noviembre(20, 8)));

            Assert.Equal(EstadoRuta.INACTIVA, ruta.Estado);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task ActivarRutaAsync_CuandoEsSuUltimoDiaYTodaviaNoLlego_LaActiva() {
            var ruta = RutaTestData.CrearRuta(
                id: 15,
                usuarioId: UsuarioId,
                estado: EstadoRuta.INACTIVA,
                fechaFin: new DateTime(2026, 11, 20),
                horaLlegada: new TimeOnly(9, 30));
            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(15))
                .ReturnsAsync(ruta);

            await _servicio.ActivarRutaAsync(15, UsuarioId, Noviembre(20, 9, 29));

            Assert.Equal(EstadoRuta.ACTIVA, ruta.Estado);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task ObtenerRutasDeUnUsuarioAsync_MarcaComoVencidasSoloLasQueYaPasaronSuFechaHasta() {
            var vencida = RutaTestData.CrearRuta(id: 1, estado: EstadoRuta.INACTIVA, fechaFin: new DateTime(2026, 11, 19));
            var vigente = RutaTestData.CrearRuta(id: 2, fechaFin: new DateTime(2026, 11, 30));
            var sinFin = RutaTestData.CrearRuta(id: 3, fechaFin: null);
            _rutaRepositorioMock
                .Setup(r => r.ObtenerRutasDeUnUsuarioAsync(UsuarioId))
                .ReturnsAsync([vencida, vigente, sinFin]);

            List<RutaDTO> rutas = await _servicio.ObtenerRutasDeUnUsuarioAsync(UsuarioId, Noviembre(20, 8));

            Assert.True(rutas.Single(r => r.Id == 1).Vencida);
            Assert.False(rutas.Single(r => r.Id == 2).Vencida);
            Assert.False(rutas.Single(r => r.Id == 3).Vencida);
        }

        #endregion

        #region Obtener para editar

        [Fact]
        public async Task ObtenerParaEditarAsync_CuandoLaRutaEsDelUsuario_DevuelveSusDatos() {
            Ruta ruta = CrearRutaParaEditar();
            _rutaRepositorioMock.Setup(r => r.ObtenerPorIdAsync(ruta.Id)).ReturnsAsync(ruta);

            EditarRutaDTO dto = await _servicio.ObtenerParaEditarAsync(ruta.Id, UsuarioId);

            Assert.Equal(ruta.Id, dto.Id);
            Assert.Equal("Origen", dto.DireccionOrigen.Texto);
            Assert.Equal("Destino", dto.DireccionDestino.Texto);
            Assert.Equal(ruta.PesoMax, dto.PesoMax);
            Assert.Equal(ruta.CapacidadMaxPaquetes, dto.CapacidadMaxPaquetes);
            Assert.Equal(ruta.HoraSalida, dto.HoraSalida);
            Assert.Equal([DayOfWeek.Monday, DayOfWeek.Wednesday], dto.RutaDias.Select(d => d.DiaSemana));
            Assert.Single(dto.Paradas);
        }

        [Fact]
        public async Task ObtenerParaEditarAsync_CuandoLaRutaNoExiste_LanzaRutaNoEncontradaException() {
            _rutaRepositorioMock.Setup(r => r.ObtenerPorIdAsync(99)).ReturnsAsync((Ruta?)null);

            await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => _servicio.ObtenerParaEditarAsync(99, UsuarioId));
        }

        [Fact]
        public async Task ObtenerParaEditarAsync_CuandoLaRutaEstaEliminada_LanzaRutaNoEncontradaException() {
            Ruta ruta = CrearRutaParaEditar();
            ruta.Estado = EstadoRuta.ELIMINADA;
            _rutaRepositorioMock.Setup(r => r.ObtenerPorIdAsync(ruta.Id)).ReturnsAsync(ruta);

            await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => _servicio.ObtenerParaEditarAsync(ruta.Id, UsuarioId));
        }

        [Fact]
        public async Task ObtenerParaEditarAsync_CuandoLaRutaEsDeOtroUsuario_LanzaUnauthorizedAccessException() {
            Ruta ruta = CrearRutaParaEditar();
            ruta.UsuarioId = 999;
            _rutaRepositorioMock.Setup(r => r.ObtenerPorIdAsync(ruta.Id)).ReturnsAsync(ruta);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _servicio.ObtenerParaEditarAsync(ruta.Id, UsuarioId));
        }

        #endregion

        #region Editar ruta: caso feliz

        [Fact]
        public async Task EditarRutaAsync_CuandoLosDatosSonValidos_ActualizaLosCamposYGuarda() {
            Ruta ruta = ConfigurarRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.PesoMax = 80;
            dto.CapacidadMaxPaquetes = 6;
            dto.FechaInicio = new DateTime(2026, 12, 1);
            dto.FechaFin = new DateTime(2027, 3, 1);
            dto.HoraSalida = new TimeOnly(7, 30);
            dto.HoraLlegada = new TimeOnly(13, 0);

            ResultadoEdicionRutaDTO resultado = await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Equal(80, ruta.PesoMax);
            Assert.Equal(6, ruta.CapacidadMaxPaquetes);
            Assert.Equal(new DateTime(2026, 12, 1), ruta.FechaInicio);
            Assert.Equal(new DateTime(2027, 3, 1), ruta.FechaFin);
            Assert.Equal(new TimeOnly(7, 30), ruta.HoraSalida);
            Assert.Equal(new TimeOnly(13, 0), ruta.HoraLlegada);
            Assert.Empty(resultado.Avisos);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoLaRutaEstaInactiva_PermiteEditarlaYNoCambiaSuEstado() {
            Ruta ruta = ConfigurarRutaParaEditar();
            ruta.Estado = EstadoRuta.INACTIVA;
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.PesoMax = 50;

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Equal(50, ruta.PesoMax);
            Assert.Equal(EstadoRuta.INACTIVA, ruta.Estado);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambianLosDias_ReemplazaLosDiasYBorraLosAnteriores() {
            Ruta ruta = ConfigurarRutaParaEditar();
            List<RutaDia> diasAnteriores = ruta.RutaDias.ToList();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.RutaDias = [new RutaDiaDTO { DiaSemana = DayOfWeek.Friday }];

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Equal([DayOfWeek.Friday], ruta.RutaDias.Select(d => d.DiaSemana));
            _rutaRepositorioMock.Verify(r => r.EliminarDias(
                It.Is<IEnumerable<RutaDia>>(d => d.SequenceEqual(diasAnteriores))), Times.Once);
        }

        #endregion

        #region Editar ruta: direcciones, paradas y recorrido

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaElOrigen_CreaUnaDireccionNuevaSinModificarLaAnterior() {
            Ruta ruta = ConfigurarRutaParaEditar();
            Direccion origenAnterior = ruta.DireccionOrigen;
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.DireccionOrigen = CrearDireccion(-34.50000m, -58.50000m, "Nuevo origen");

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.NotSame(origenAnterior, ruta.DireccionOrigen);
            Assert.Equal("Nuevo origen", ruta.DireccionOrigen.Texto);
            Assert.Equal(-34.50000m, ruta.DireccionOrigen.Latitud);
            // La direccion anterior la siguen usando los viajes ya creados: no se toca.
            Assert.Equal("Origen", origenAnterior.Texto);
            Assert.Equal(-34.60372m, origenAnterior.Latitud);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaElDestino_CreaUnaDireccionNuevaSinModificarLaAnterior() {
            Ruta ruta = ConfigurarRutaParaEditar();
            Direccion destinoAnterior = ruta.DireccionDestino;
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.DireccionDestino = CrearDireccion(-34.95000m, -57.95000m, "Nuevo destino");

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.NotSame(destinoAnterior, ruta.DireccionDestino);
            Assert.Equal("Nuevo destino", ruta.DireccionDestino.Texto);
            Assert.Equal("Destino", destinoAnterior.Texto);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaElOrigen_RecalculaElRecorrido() {
            Ruta ruta = ConfigurarRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.DireccionOrigen = CrearDireccion(-34.50000m, -58.50000m, "Nuevo origen");

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            _recorridoServicioMock.Verify(r => r.CalcularAsync(
                It.Is<RecorridoDTO>(x => x.Origen.Texto == "Nuevo origen"), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(15000, ruta.DistanciaMetros);
            Assert.Equal(1200, ruta.DuracionSegundos);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoNoCambiaElRecorrido_NoRecalculaNiCambiaLasDirecciones() {
            Ruta ruta = ConfigurarRutaParaEditar();
            Direccion origen = ruta.DireccionOrigen;
            Direccion destino = ruta.DireccionDestino;
            List<Parada> paradas = ruta.Paradas.ToList();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.PesoMax = 70;

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Same(origen, ruta.DireccionOrigen);
            Assert.Same(destino, ruta.DireccionDestino);
            Assert.Equal(paradas, ruta.Paradas);
            Assert.Equal(5000, ruta.DistanciaMetros);
            _recorridoServicioMock.Verify(r => r.CalcularAsync(It.IsAny<RecorridoDTO>(), It.IsAny<CancellationToken>()), Times.Never);
            _rutaRepositorioMock.Verify(r => r.EliminarParadas(It.IsAny<IEnumerable<Parada>>()), Times.Never);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoLaRutaNoTeniaDistanciaCalculada_RecalculaElRecorrido() {
            Ruta ruta = ConfigurarRutaParaEditar();
            ruta.DistanciaMetros = null;

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, DtoDesde(ruta));

            _recorridoServicioMock.Verify(r => r.CalcularAsync(It.IsAny<RecorridoDTO>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(15000, ruta.DistanciaMetros);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambianLasParadas_LasReemplazaBorraLasAnterioresYRecalcula() {
            Ruta ruta = ConfigurarRutaParaEditar();
            List<Parada> paradasAnteriores = ruta.Paradas.ToList();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.Paradas =
            [
                CrearParada(-34.75000m, -58.15000m, 1),
                CrearParada(-34.85000m, -58.05000m, 2)
            ];

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Equal(2, ruta.Paradas.Count);
            Assert.Equal([1, 2], ruta.Paradas.Select(p => p.Orden));
            Assert.Equal(-34.75000m, ruta.Paradas[0].Direccion.Latitud);
            Assert.DoesNotContain(paradasAnteriores[0], ruta.Paradas);
            _rutaRepositorioMock.Verify(r => r.EliminarParadas(
                It.Is<IEnumerable<Parada>>(p => p.SequenceEqual(paradasAnteriores))), Times.Once);
            _recorridoServicioMock.Verify(r => r.CalcularAsync(It.IsAny<RecorridoDTO>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoSeQuitanTodasLasParadas_BorraLasAnteriores() {
            Ruta ruta = ConfigurarRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.Paradas = [];

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Empty(ruta.Paradas);
            _rutaRepositorioMock.Verify(r => r.EliminarParadas(It.Is<IEnumerable<Parada>>(p => p.Count() == 1)), Times.Once);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoElServicioDeRecorridoFalla_GuardaIgualLaRuta() {
            Ruta ruta = ConfigurarRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.DireccionOrigen = CrearDireccion(-34.50000m, -58.50000m, "Nuevo origen");
            _recorridoServicioMock
                .Setup(r => r.CalcularAsync(It.IsAny<RecorridoDTO>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RutaNoEncontradaException("ORS caído"));

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Equal("Nuevo origen", ruta.DireccionOrigen.Texto);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        #endregion

        #region Editar ruta: viajes ya creados

        [Fact]
        public async Task EditarRutaAsync_CuandoHayViajesCreados_NoLosModifica() {
            Ruta ruta = ConfigurarRutaParaEditar();
            Viaje viaje = AgregarViaje(ruta, DateTime.Today.AddDays(1).AddHours(8), conPaquete: true);
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.PesoMax = 10;
            dto.CapacidadMaxPaquetes = 1;
            dto.DireccionOrigen = CrearDireccion(-34.50000m, -58.50000m, "Nuevo origen");

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Equal(100, viaje.PesoMax);
            Assert.Equal(10, viaje.CapacidadMaxPaquetes);
            Assert.Equal("Origen", viaje.DireccionOrigen.Texto);
            Assert.Equal(-34.60372m, viaje.DireccionOrigen.Latitud);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaLaCapacidadYHayViajesConPaquetes_AvisaQueMantienenLosValoresAnteriores() {
            Ruta ruta = ConfigurarRutaParaEditar();
            AgregarViaje(ruta, DateTime.Today.AddDays(1).AddHours(8), conPaquete: true);
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.CapacidadMaxPaquetes = 2;

            ResultadoEdicionRutaDTO resultado = await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            string aviso = Assert.Single(resultado.Avisos);
            Assert.Contains("1 viaje(s) con paquetes asignados", aviso);
            Assert.Equal(2, ruta.CapacidadMaxPaquetes);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaElPesoYHayViajesConPaquetes_Avisa() {
            Ruta ruta = ConfigurarRutaParaEditar();
            AgregarViaje(ruta, DateTime.Today.AddDays(1).AddHours(8), conPaquete: true);
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.PesoMax = 1;

            ResultadoEdicionRutaDTO resultado = await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Contains("paquetes asignados", Assert.Single(resultado.Avisos));
            Assert.Equal(1, ruta.PesoMax);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoLasAsignacionesDelViajeEstanCanceladas_AvisaSoloQueLosViajesCreadosNoCambian() {
            Ruta ruta = ConfigurarRutaParaEditar();
            Viaje viaje = AgregarViaje(ruta, DateTime.Today.AddDays(1).AddHours(8), conPaquete: true);
            viaje.Asignaciones[0].Estado = EstadoViaje.CANCELADO;
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.CapacidadMaxPaquetes = 2;

            ResultadoEdicionRutaDTO resultado = await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            string aviso = Assert.Single(resultado.Avisos);
            Assert.Contains("ya creados mantienen los datos anteriores", aviso);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoSoloHayViajesPasados_NoAvisa() {
            Ruta ruta = ConfigurarRutaParaEditar();
            AgregarViaje(ruta, DateTime.Today.AddDays(-2).AddHours(8), conPaquete: true);
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.CapacidadMaxPaquetes = 2;

            ResultadoEdicionRutaDTO resultado = await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Empty(resultado.Avisos);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaLaHoraDeSalidaYHayUnViajeCreado_LanzaValidacionRutaExceptionYNoGuarda() {
            Ruta ruta = ConfigurarRutaParaEditar();
            DateTime manana = DateTime.Today.AddDays(1);
            AgregarViaje(ruta, manana.AddHours(8), conPaquete: true);
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.HoraSalida = new TimeOnly(9, 0);

            var excepcion = await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));

            Assert.Contains("hora de salida", excepcion.Message);
            Assert.Contains(manana.ToString("dd/MM/yyyy"), excepcion.Message);
            Assert.Equal(new TimeOnly(8, 0), ruta.HoraSalida);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaLaHoraDeSalidaYHayUnViajeInactivoCreado_LanzaValidacionRutaException() {
            Ruta ruta = ConfigurarRutaParaEditar();
            Viaje viaje = AgregarViaje(ruta, DateTime.Today.AddDays(1).AddHours(8), conPaquete: false);
            viaje.Estado = EstadoActividadViaje.INACTIVO;
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.HoraSalida = new TimeOnly(9, 0);

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaLaHoraDeSalidaYHayUnViajeDeHoyQueYaSalio_LanzaValidacionRutaException() {
            // Si ya salio hoy a las 08:00 y la paso a las 20:00, el materializador crearia otro viaje hoy.
            Ruta ruta = ConfigurarRutaParaEditar();
            AgregarViaje(ruta, DateTime.Today.AddHours(8), conPaquete: false);
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.HoraSalida = new TimeOnly(20, 0);
            dto.HoraLlegada = new TimeOnly(23, 0);

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaLaHoraDeSalidaYSoloHayViajesDeDiasAnteriores_LaCambia() {
            Ruta ruta = ConfigurarRutaParaEditar();
            AgregarViaje(ruta, DateTime.Today.AddDays(-1).AddHours(8), conPaquete: true);
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.HoraSalida = new TimeOnly(9, 0);

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Equal(new TimeOnly(9, 0), ruta.HoraSalida);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoCambiaSoloLaHoraDeLlegadaConViajesCreados_LaCambia() {
            Ruta ruta = ConfigurarRutaParaEditar();
            AgregarViaje(ruta, DateTime.Today.AddDays(1).AddHours(8), conPaquete: true);
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.HoraLlegada = new TimeOnly(14, 0);

            await _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto);

            Assert.Equal(new TimeOnly(14, 0), ruta.HoraLlegada);
        }

        #endregion

        #region Editar ruta: errores

        [Fact]
        public async Task EditarRutaAsync_CuandoLaRutaNoExiste_LanzaRutaNoEncontradaExceptionYNoGuarda() {
            Ruta ruta = CrearRutaParaEditar();
            _rutaRepositorioMock.Setup(r => r.ObtenerParaEditarAsync(ruta.Id)).ReturnsAsync((Ruta?)null);

            await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, DtoDesde(ruta)));

            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoLaRutaEstaEliminada_LanzaRutaNoEncontradaException() {
            Ruta ruta = ConfigurarRutaParaEditar();
            ruta.Estado = EstadoRuta.ELIMINADA;

            await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, DtoDesde(ruta)));
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoLaRutaEsDeOtroUsuario_LanzaUnauthorizedAccessExceptionYNoModifica() {
            Ruta ruta = ConfigurarRutaParaEditar();
            ruta.UsuarioId = 999;
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.PesoMax = 1;

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));

            Assert.Equal(100, ruta.PesoMax);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoElOrigenYElDestinoSonIguales_LanzaValidacionRutaExceptionSinBuscarLaRuta() {
            Ruta ruta = CrearRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.DireccionDestino = CrearDireccion(dto.DireccionOrigen.Latitud, dto.DireccionOrigen.Longitud);

            var excepcion = await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));

            Assert.Equal("El origen y el destino deben ser diferentes.", excepcion.Message);
            _rutaRepositorioMock.Verify(r => r.ObtenerParaEditarAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoNoHayDias_LanzaValidacionRutaException() {
            Ruta ruta = CrearRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.RutaDias = [];

            var excepcion = await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));

            Assert.Equal("Debe seleccionar al menos un día.", excepcion.Message);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoLaCapacidadEsCero_LanzaValidacionRutaException() {
            Ruta ruta = CrearRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.CapacidadMaxPaquetes = 0;

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoLaHoraDeSalidaNoEsAnteriorALaDeLlegada_LanzaValidacionRutaException() {
            Ruta ruta = CrearRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.HoraLlegada = dto.HoraSalida;

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoLaFechaFinEsAnteriorALaDeInicio_LanzaValidacionRutaException() {
            Ruta ruta = CrearRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.FechaFin = dto.FechaInicio.AddDays(-1);

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoUnaParadaCoincideConElDestino_LanzaValidacionRutaException() {
            Ruta ruta = CrearRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.Paradas = [CrearParada(dto.DireccionDestino.Latitud, dto.DireccionDestino.Longitud, 1)];

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoElOrdenDeLasParadasNoEsConsecutivo_LanzaValidacionRutaException() {
            Ruta ruta = CrearRutaParaEditar();
            EditarRutaDTO dto = DtoDesde(ruta);
            dto.Paradas = [CrearParada(-34.70000m, -58.20000m, 2)];

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, ruta.Id, dto));
        }

        #endregion

        #region Helpers

        private async Task AssertValidacionFallaAsync(CrearRutaDTO dto, string mensajeEsperado) {
            var excepcion = await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.CrearRutaAsync(UsuarioId, dto));

            Assert.Equal(mensajeEsperado, excepcion.Message);
            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Never);
        }

        private void ConfigurarUsuario(Usuario usuario) {
            _usuarioRepositorioMock
                .Setup(r => r.BuscarPorIdAsync(UsuarioId))
                .ReturnsAsync(usuario);
        }

        private static Usuario CrearUsuario() {
            return new Usuario {
                Id = UsuarioId,
                Nombre = "test",
                Apellido = "unitario",
                Email = "test@test.com"
            };
        }

        private static DireccionDTO CrearDireccion(decimal latitud, decimal longitud, string texto = "Direccion de prueba") {
            return new DireccionDTO {
                Texto = texto,
                Latitud = latitud,
                Longitud = longitud
            };
        }

        private static ParadaDTO CrearParada(decimal latitud, decimal longitud, int orden) {
            return new ParadaDTO {
                Direccion = CrearDireccion(latitud, longitud),
                Orden = orden
            };
        }

        // Ruta de lunes y miercoles 08:00-12:00 con una parada y el recorrido ya calculado.
        private static Ruta CrearRutaParaEditar() {
            Direccion origen = new() { Id = 101, Texto = "Origen", Latitud = -34.60372m, Longitud = -58.38160m };
            Direccion destino = new() { Id = 102, Texto = "Destino", Latitud = -34.90111m, Longitud = -57.92220m };
            Direccion direccionParada = new() { Id = 103, Texto = "Parada", Latitud = -34.70000m, Longitud = -58.20000m };

            return new Ruta {
                Id = 20,
                UsuarioId = UsuarioId,
                Estado = EstadoRuta.ACTIVA,
                DireccionOrigenId = origen.Id,
                DireccionOrigen = origen,
                DireccionDestinoId = destino.Id,
                DireccionDestino = destino,
                PesoMax = 100,
                CapacidadMaxPaquetes = 10,
                FechaInicio = new DateTime(2026, 11, 1),
                FechaFin = new DateTime(2026, 12, 31),
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(12, 0),
                DistanciaMetros = 5000,
                DuracionSegundos = 600,
                RutaDias =
                [
                    new RutaDia { Id = 1, DiaSemana = DayOfWeek.Monday },
                    new RutaDia { Id = 2, DiaSemana = DayOfWeek.Wednesday }
                ],
                Paradas =
                [
                    new Parada { Id = 1, RutaId = 20, DireccionId = direccionParada.Id, Direccion = direccionParada, Orden = 1 }
                ]
            };
        }

        private Ruta ConfigurarRutaParaEditar() {
            Ruta ruta = CrearRutaParaEditar();
            _rutaRepositorioMock.Setup(r => r.ObtenerParaEditarAsync(ruta.Id)).ReturnsAsync(ruta);
            return ruta;
        }

        // DTO con los mismos datos que tiene la ruta (como llega del formulario si no se cambia nada).
        private static EditarRutaDTO DtoDesde(Ruta ruta) => new() {
            Id = ruta.Id,
            DireccionOrigen = CrearDireccion(ruta.DireccionOrigen.Latitud, ruta.DireccionOrigen.Longitud, ruta.DireccionOrigen.Texto),
            DireccionDestino = CrearDireccion(ruta.DireccionDestino.Latitud, ruta.DireccionDestino.Longitud, ruta.DireccionDestino.Texto),
            PesoMax = ruta.PesoMax,
            CapacidadMaxPaquetes = ruta.CapacidadMaxPaquetes,
            FechaInicio = ruta.FechaInicio,
            FechaFin = ruta.FechaFin,
            HoraSalida = ruta.HoraSalida,
            HoraLlegada = ruta.HoraLlegada,
            RutaDias = ruta.RutaDias.Select(d => new RutaDiaDTO { DiaSemana = d.DiaSemana }).ToList(),
            Paradas = ruta.Paradas.Select(p => new ParadaDTO {
                Orden = p.Orden,
                Direccion = CrearDireccion(p.Direccion.Latitud, p.Direccion.Longitud, p.Direccion.Texto)
            }).ToList()
        };

        // Viaje ya creado (snapshot) de la ruta: comparte las direcciones, como hace SnapshotDeRuta.
        private static Viaje AgregarViaje(Ruta ruta, DateTime fechaSalida, bool conPaquete) {
            Viaje viaje = new() {
                Id = ruta.Viajes.Count + 1,
                RutaId = ruta.Id,
                Ruta = ruta,
                DireccionOrigenId = ruta.DireccionOrigenId,
                DireccionOrigen = ruta.DireccionOrigen,
                DireccionDestinoId = ruta.DireccionDestinoId,
                DireccionDestino = ruta.DireccionDestino,
                PesoMax = ruta.PesoMax,
                CapacidadMaxPaquetes = ruta.CapacidadMaxPaquetes,
                FechaSalida = fechaSalida,
                FechaLlegada = fechaSalida.AddHours(4),
                Estado = EstadoActividadViaje.ACTIVO
            };

            if (conPaquete) {
                viaje.Asignaciones.Add(new Asignacion {
                    Id = 1,
                    ViajeId = viaje.Id,
                    Viaje = viaje,
                    Estado = EstadoViaje.PENDIENTE,
                    Envio = new Envio { Paquete = new Paquete { PesoKg = 5 } }
                });
            }

            ruta.Viajes.Add(viaje);
            return viaje;
        }

        private static CrearRutaDTO CrearRutaValida() {
            return new CrearRutaDTO {
                DireccionOrigen = CrearDireccion(-34.60372m, -58.38160m, "Origen"),
                DireccionDestino = CrearDireccion(-34.90111m, -57.92220m, "Destino"),
                PesoMax = 100,
                CapacidadMaxPaquetes = 10,
                FechaInicio = new DateTime(2026, 11, 1),
                FechaFin = new DateTime(2026, 12, 1),
                RutaDias =
                [
                    new RutaDiaDTO { DiaSemana = DayOfWeek.Monday },
                    new RutaDiaDTO { DiaSemana = DayOfWeek.Wednesday }
                ],
                Paradas =
                [
                    CrearParada(-34.70000m, -58.20000m, 1),
                    CrearParada(-34.80000m, -58.10000m, 2)
                ],
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(12, 0)
            };
        }

        #endregion
    }
}