using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Entidades.Excepciones;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios
{
    public class RutaServicioTests
    {
        private const int UsuarioId = 1;

        private readonly Mock<IRutaRepositorio> _rutaRepositorioMock;
        private readonly Mock<IUsuarioRepositorio> _usuarioRepositorioMock;
        private readonly RutaServicio _servicio;

        public RutaServicioTests()
        {
            _rutaRepositorioMock = new Mock<IRutaRepositorio>();
            _usuarioRepositorioMock = new Mock<IUsuarioRepositorio>();
            _servicio = new RutaServicio(
                _rutaRepositorioMock.Object,
                _usuarioRepositorioMock.Object,
                MapperFactory.Crear());
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
