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

namespace TpMiercoles.Tests.Servicios
{
    public class RutaServicioTests
    {
        private const int UsuarioId = 1;

        private readonly Mock<IRutaRepositorio> _rutaRepositorioMock;
        private readonly Mock<IUsuarioRepositorio> _usuarioRepositorioMock;
        private readonly RutaServicio _servicio;
        private readonly Mock<IRecorridoServicio> _recorridoServicioMock;

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
        public async Task CrearRutaAsync_CuandoLaRutaEsValida_CreaLaRutaConElUsuarioYLosDatosMapeados()
        {
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
        public async Task CrearRutaAsync_CuandoLaRutaEsValida_BuscaElUsuarioPorElIdRecibido()
        {
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, CrearRutaValida());

            _usuarioRepositorioMock.Verify(r => r.BuscarPorIdAsync(UsuarioId), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLaRutaNoTieneParadas_CreaLaRuta()
        {
            var dto = CrearRutaValida();
            dto.Paradas = [];
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoNoHayFechaFin_CreaLaRuta()
        {
            var dto = CrearRutaValida();
            dto.FechaFin = null;
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLaFechaFinEsIgualALaFechaInicio_CreaLaRuta()
        {
            var dto = CrearRutaValida();
            dto.FechaFin = dto.FechaInicio;
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLosOrdenesDeParadasEstanDesordenadosPeroSonConsecutivos_CreaLaRuta()
        {
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
        public async Task CrearRutaAsync_CuandoElOrigenYElDestinoSoloDifierenEnLaLatitud_CreaLaRuta()
        {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m);
            dto.DireccionDestino = CrearDireccion(10.12346m, 20.12345m);
            dto.Paradas = [];
            ConfigurarUsuario(CrearUsuario());

            await _servicio.CrearRutaAsync(UsuarioId, dto);

            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Once);
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoElOrigenYElDestinoSoloDifierenEnLaLongitud_CreaLaRuta()
        {
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
        public async Task CrearRutaAsync_CuandoElOrigenYElDestinoTienenLasMismasCoordenadas_LanzaValidacionRutaException()
        {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m);
            dto.DireccionDestino = CrearDireccion(10.12345m, 20.12345m);
            dto.Paradas = [];

            await AssertValidacionFallaAsync(dto, "El origen y el destino deben ser diferentes.");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoElOrigenYElDestinoTienenLasMismasCoordenadasPeroDistintoTexto_LanzaValidacionRutaException()
        {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m, "Calle A 123");
            dto.DireccionDestino = CrearDireccion(10.12345m, 20.12345m, "Calle B 456");
            dto.Paradas = [];

            await AssertValidacionFallaAsync(dto, "El origen y el destino deben ser diferentes.");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLasCoordenadasDifierenSoloDesdeElSextoDecimal_LasConsideraIguales()
        {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.123451m, 20.123451m);
            dto.DireccionDestino = CrearDireccion(10.123454m, 20.123454m);
            dto.Paradas = [];

            await AssertValidacionFallaAsync(dto, "El origen y el destino deben ser diferentes.");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoLasCoordenadasEstanEnElPuntoMedioDelRedondeo_UsaRedondeoAlParMasCercano()
        {
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
        public async Task CrearRutaAsync_CuandoNoHayDiasSeleccionados_LanzaValidacionRutaException()
        {
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
        public async Task CrearRutaAsync_CuandoElPesoMaximoNoEsMayorACero_LanzaValidacionRutaException(double pesoMax)
        {
            var dto = CrearRutaValida();
            dto.PesoMax = pesoMax;

            await AssertValidacionFallaAsync(dto, "Peso y capacidad deben ser mayores que cero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public async Task CrearRutaAsync_CuandoLaCapacidadMaximaNoEsMayorACero_LanzaValidacionRutaException(int capacidad)
        {
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
            int horaLlegada)
        {
            var dto = CrearRutaValida();
            dto.HoraSalida = new TimeOnly(horaSalida, 0);
            dto.HoraLlegada = new TimeOnly(horaLlegada, 0);

            await AssertValidacionFallaAsync(dto, "La hora de salida debe ser anterior a la de llegada.");
        }

        #endregion

        #region Fechas

        [Fact]
        public async Task CrearRutaAsync_CuandoLaFechaFinEsAnteriorALaFechaInicio_LanzaValidacionRutaException()
        {
            var dto = CrearRutaValida();
            dto.FechaInicio = new DateTime(2026, 11, 10);
            dto.FechaFin = new DateTime(2026, 11, 9);

            await AssertValidacionFallaAsync(dto, "La fecha de fin no puede ser anterior a la de inicio.");
        }

        #endregion

        #region Paradas

        [Fact]
        public async Task CrearRutaAsync_CuandoUnaParadaTieneLasCoordenadasDelOrigen_LanzaValidacionRutaException()
        {
            var dto = CrearRutaValida();
            dto.Paradas =
            [
                CrearParada(dto.DireccionOrigen.Latitud, dto.DireccionOrigen.Longitud, 1)
            ];

            await AssertValidacionFallaAsync(dto, "La parada no puede ser igual a la direccion de origen o destino");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoUnaParadaTieneLasCoordenadasDelDestino_LanzaValidacionRutaException()
        {
            var dto = CrearRutaValida();
            dto.Paradas =
            [
                CrearParada(dto.DireccionDestino.Latitud, dto.DireccionDestino.Longitud, 1)
            ];

            await AssertValidacionFallaAsync(dto, "La parada no puede ser igual a la direccion de origen o destino");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoUnaParadaSoloDifiereDelOrigenDesdeElSextoDecimal_LanzaValidacionRutaException()
        {
            var dto = CrearRutaValida();
            dto.DireccionOrigen = CrearDireccion(10.12345m, 20.12345m);
            dto.DireccionDestino = CrearDireccion(30.00000m, 40.00000m);
            dto.Paradas = [CrearParada(10.123451m, 20.123451m, 1)];

            await AssertValidacionFallaAsync(dto, "La parada no puede ser igual a la direccion de origen o destino");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoDosParadasTienenLasMismasCoordenadas_LanzaValidacionRutaException()
        {
            var dto = CrearRutaValida();
            dto.Paradas =
            [
                CrearParada(-34.60500m, -58.38300m, 1),
                CrearParada(-34.60500m, -58.38300m, 2)
            ];

            await AssertValidacionFallaAsync(dto, "No se pueden repetir paradas.");
        }

        [Fact]
        public async Task CrearRutaAsync_CuandoDosParadasSoloDifierenDesdeElSextoDecimal_LanzaValidacionRutaException()
        {
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
            int[] ordenes)
        {
            var dto = CrearRutaValida();
            dto.Paradas = ordenes
                .Select((orden, indice) => CrearParada(-34.60500m - (indice * 0.001m), -58.38300m, orden))
                .ToList();

            await AssertValidacionFallaAsync(dto, "El orden de las paradas debe ser consecutivo, empezando en 1.");
        }

        #endregion

        #region Orden de operaciones

        [Fact]
        public async Task CrearRutaAsync_CuandoLaValidacionFalla_NoBuscaAlUsuarioNiCreaLaRuta()
        {
            var dto = CrearRutaValida();
            dto.RutaDias = [];

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.CrearRutaAsync(UsuarioId, dto));

            _usuarioRepositorioMock.Verify(r => r.BuscarPorIdAsync(It.IsAny<int>()), Times.Never);
            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Never);
        }

        #endregion

        #region Desactivar y Activar ruta

        [Fact]
        public async Task DesactivarRutaAsync_CuandoRutaExisteYEsDelUsuario_CambiaEstadoAInactivaYGuardaCambios()
        {
            const int rutaId = 15;
            var ruta = new Ruta
            {
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
        public async Task DesactivarRutaAsync_CuandoRutaNoExiste_LanzaRutaNoEncontradaException()
        {
            const int rutaId = 99;
            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync((Ruta?)null);

            await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => _servicio.DesactivarRutaAsync(rutaId, UsuarioId));

            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task DesactivarRutaAsync_CuandoRutaNoPerteneceAlUsuario_LanzaUnauthorizedAccessException()
        {
            const int rutaId = 15;
            const int otroUsuarioId = 999;
            var ruta = new Ruta
            {
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
        public async Task ActivarRutaAsync_CuandoRutaExisteYEsDelUsuario_CambiaEstadoAActivaYGuardaCambios()
        {
            const int rutaId = 15;
            var ruta = new Ruta
            {
                Id = rutaId,
                UsuarioId = UsuarioId,
                Estado = EstadoRuta.INACTIVA
            };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            await _servicio.ActivarRutaAsync(rutaId, UsuarioId);

            Assert.Equal(EstadoRuta.ACTIVA, ruta.Estado);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        #endregion

        #region Editar ruta

        [Fact]
        public async Task ObtenerParaEditarAsync_CuandoRutaExisteYEsDelUsuario_DevuelveDtoMapeado()
        {
            const int rutaId = 20;
            var ruta = new Ruta
            {
                Id = rutaId,
                UsuarioId = UsuarioId,
                DireccionOrigen = new Direccion { Texto = "Origen", Latitud = -34.60m, Longitud = -58.38m },
                DireccionDestino = new Direccion { Texto = "Destino", Latitud = -34.90m, Longitud = -57.92m },
                PesoMax = 50,
                CapacidadMaxPaquetes = 5,
                FechaInicio = new DateTime(2026, 11, 1),
                FechaFin = new DateTime(2026, 12, 1),
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(12, 0)
            };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            var resultado = await _servicio.ObtenerParaEditarAsync(rutaId, UsuarioId);

            Assert.NotNull(resultado);
            Assert.Equal(rutaId, resultado.Id);
            Assert.Equal(50, resultado.PesoMax);
            Assert.Equal(5, resultado.CapacidadMaxPaquetes);
            Assert.Equal("Origen", resultado.DireccionOrigen.Texto);
        }

        [Fact]
        public async Task ObtenerParaEditarAsync_CuandoRutaNoExiste_LanzaRutaNoEncontradaException()
        {
            const int rutaId = 99;
            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync((Ruta?)null);

            await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => _servicio.ObtenerParaEditarAsync(rutaId, UsuarioId));
        }

        [Fact]
        public async Task ObtenerParaEditarAsync_CuandoRutaNoEsDelUsuario_LanzaUnauthorizedAccessException()
        {
            const int rutaId = 20;
            var ruta = new Ruta { Id = rutaId, UsuarioId = 999 };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _servicio.ObtenerParaEditarAsync(rutaId, UsuarioId));
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoDatosSonValidos_ActualizaRutaYGuardaCambios()
        {
            const int rutaId = 20;
            var ruta = new Ruta
            {
                Id = rutaId,
                UsuarioId = UsuarioId,
                DireccionOrigen = new Direccion { Texto = "Viejo Origen", Latitud = -34.60m, Longitud = -58.38m },
                DireccionDestino = new Direccion { Texto = "Viejo Destino", Latitud = -34.90m, Longitud = -57.92m },
                PesoMax = 50,
                CapacidadMaxPaquetes = 5,
                FechaInicio = new DateTime(2026, 11, 1),
                FechaFin = new DateTime(2026, 12, 1),
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(12, 0)
            };

            var dto = new EditarRutaDTO
            {
                Id = rutaId,
                DireccionOrigen = CrearDireccion(-34.60372m, -58.38160m, "Nuevo Origen"),
                DireccionDestino = CrearDireccion(-34.90111m, -57.92220m, "Nuevo Destino"),
                PesoMax = 80,
                CapacidadMaxPaquetes = 8,
                FechaInicio = new DateTime(2026, 11, 5),
                FechaFin = new DateTime(2026, 12, 5),
                RutaDias = [new RutaDiaDTO { DiaSemana = DayOfWeek.Tuesday }],
                Paradas = [CrearParada(-34.70000m, -58.20000m, 1)],
                HoraSalida = new TimeOnly(9, 0),
                HoraLlegada = new TimeOnly(13, 0)
            };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            await _servicio.EditarRutaAsync(UsuarioId, rutaId, dto);

            Assert.Equal(80, ruta.PesoMax);
            Assert.Equal(8, ruta.CapacidadMaxPaquetes);
            Assert.Equal("Nuevo Origen", ruta.DireccionOrigen.Texto);
            Assert.Equal("Nuevo Destino", ruta.DireccionDestino.Texto);
            Assert.Single(ruta.RutaDias);
            Assert.Single(ruta.Paradas);
            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoViajeComprometidoSuperaNuevaCapacidad_LanzaValidacionRutaException()
        {
            const int rutaId = 20;
            var viaje = new Viaje
            {
                Id = 1,
                FechaSalida = DateTime.Now.AddDays(1),
                Estado = EstadoActividadViaje.ACTIVO,
                Asignaciones = 
                [
                    new Asignacion { Id = 1, Estado = EstadoViaje.CONFIRMADO },
                    new Asignacion { Id = 2, Estado = EstadoViaje.CONFIRMADO }
                ]
            };

            var ruta = new Ruta
            {
                Id = rutaId,
                UsuarioId = UsuarioId,
                DireccionOrigen = new Direccion { Latitud = -34.60m, Longitud = -58.38m },
                DireccionDestino = new Direccion { Latitud = -34.90m, Longitud = -57.92m },
                Viajes = [viaje]
            };

            var dto = new EditarRutaDTO
            {
                Id = rutaId,
                DireccionOrigen = CrearDireccion(-34.60m, -58.38m, "Origen"),
                DireccionDestino = CrearDireccion(-34.90m, -57.92m, "Destino"),
                PesoMax = 100,
                CapacidadMaxPaquetes = 1,
                FechaInicio = new DateTime(2026, 11, 1),
                FechaFin = new DateTime(2026, 12, 1),
                RutaDias = [new RutaDiaDTO { DiaSemana = DayOfWeek.Monday }],
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(12, 0)
            };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, rutaId, dto));

            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task EditarRutaAsync_CuandoViajeComprometidoSuperaNuevoPeso_LanzaValidacionRutaException()
        {
            const int rutaId = 20;
            var envio = new Envio
            {
                Paquete = new Paquete { PesoKg = 30 }
            };

            var viaje = new Viaje
            {
                Id = 1,
                FechaSalida = DateTime.Now.AddDays(1),
                Estado = EstadoActividadViaje.ACTIVO,
                Asignaciones = 
                [
                    new Asignacion { Id = 1, Estado = EstadoViaje.CONFIRMADO, Envio = envio }
                ]
            };

            var ruta = new Ruta
            {
                Id = rutaId,
                UsuarioId = UsuarioId,
                DireccionOrigen = new Direccion { Latitud = -34.60m, Longitud = -58.38m },
                DireccionDestino = new Direccion { Latitud = -34.90m, Longitud = -57.92m },
                Viajes = [viaje]
            };

            var dto = new EditarRutaDTO
            {
                Id = rutaId,
                DireccionOrigen = CrearDireccion(-34.60m, -58.38m, "Origen"),
                DireccionDestino = CrearDireccion(-34.90m, -57.92m, "Destino"),
                PesoMax = 20,
                CapacidadMaxPaquetes = 5,
                FechaInicio = new DateTime(2026, 11, 1),
                FechaFin = new DateTime(2026, 12, 1),
                RutaDias = [new RutaDiaDTO { DiaSemana = DayOfWeek.Monday }],
                HoraSalida = new TimeOnly(8, 0),
                HoraLlegada = new TimeOnly(12, 0)
            };

            _rutaRepositorioMock
                .Setup(r => r.ObtenerPorIdParaModificarAsync(rutaId))
                .ReturnsAsync(ruta);

            await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.EditarRutaAsync(UsuarioId, rutaId, dto));

            _rutaRepositorioMock.Verify(r => r.GuardarCambiosAsync(), Times.Never);
        }

        #endregion

        #region Helpers

        private async Task AssertValidacionFallaAsync(CrearRutaDTO dto, string mensajeEsperado)
        {
            var excepcion = await Assert.ThrowsAsync<ValidacionRutaException>(
                () => _servicio.CrearRutaAsync(UsuarioId, dto));

            Assert.Equal(mensajeEsperado, excepcion.Message);
            _rutaRepositorioMock.Verify(r => r.CrearRutaAsync(It.IsAny<Ruta>()), Times.Never);
        }

        private void ConfigurarUsuario(Usuario usuario)
        {
            _usuarioRepositorioMock
                .Setup(r => r.BuscarPorIdAsync(UsuarioId))
                .ReturnsAsync(usuario);
        }

        private static Usuario CrearUsuario()
        {
            return new Usuario
            {
                Id = UsuarioId,
                Nombre = "test",
                Apellido = "unitario",
                Email = "test@test.com"
            };
        }

        private static DireccionDTO CrearDireccion(decimal latitud, decimal longitud, string texto = "Direccion de prueba")
        {
            return new DireccionDTO
            {
                Texto = texto,
                Latitud = latitud,
                Longitud = longitud
            };
        }

        private static ParadaDTO CrearParada(decimal latitud, decimal longitud, int orden)
        {
            return new ParadaDTO
            {
                Direccion = CrearDireccion(latitud, longitud),
                Orden = orden
            };
        }

        private static CrearRutaDTO CrearRutaValida()
        {
            return new CrearRutaDTO
            {
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
