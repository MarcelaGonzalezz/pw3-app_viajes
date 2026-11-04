using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios
{
    public class OportunidadServicioTests
    {
        // lunes 2/11/2026 07:00 en Argentina (UTC-3)
        private static readonly DateTimeOffset Ahora = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);
        private static readonly DateTime SalidaDelLunes = new(2026, 11, 2, 8, 0, 0);

        private readonly Mock<IRutaRepositorio> _rutas = new();
        private readonly Mock<IViajeRepositorio> _viajes = new();
        private readonly Mock<IEnvioRepositorio> _envios = new();
        private readonly Mock<IServicioRutas> _servicioRutas = new();
        private readonly OportunidadServicio _servicio;

        public OportunidadServicioTests()
        {
            _servicioRutas
                .Setup(s => s.CalcularRutaAsync(
                    It.IsAny<Coordenada>(),
                    It.IsAny<Coordenada>(),
                    It.IsAny<IReadOnlyList<Coordenada>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultadoRuta(10_000, 900));

            _servicio = new OportunidadServicio(
                _rutas.Object,
                _viajes.Object,
                _envios.Object,
                _servicioRutas.Object,
                new RelojFijo(Ahora));
        }

        [Fact]
        public async Task ListarSalidasAsync_ConRutaActiva_DevuelveSuProximaSalida()
        {
            Ruta ruta = CrearRutaDelLunes();
            _rutas.Setup(r => r.ObtenerRutasActivasDeUnUsuarioAsync(1)).ReturnsAsync([ruta]);
            _viajes.Setup(v => v.ObtenerPorRutaYFechaAsync(1, SalidaDelLunes)).ReturnsAsync((Viaje?)null);

            List<SalidaDeRutaDTO> salidas = await _servicio.ListarSalidasAsync(1);

            SalidaDeRutaDTO salida = Assert.Single(salidas);
            Assert.Equal(1, salida.RutaId);
            Assert.Null(salida.ViajeId);
            Assert.Equal(SalidaDelLunes, salida.FechaSalida);
            Assert.Equal(SalidaDelLunes.AddMinutes(90), salida.FechaLlegada);
        }

        [Fact]
        public async Task ListarAsync_SinViajeMaterializado_UsaLaCapacidadDeLaRuta()
        {
            Ruta ruta = CrearRutaDelLunes();
            _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);
            _viajes.Setup(v => v.ObtenerPorRutaYFechaAsync(1, SalidaDelLunes)).ReturnsAsync((Viaje?)null);
            _envios.Setup(e => e.ListarDisponiblesAsync(SalidaDelLunes, 1)).ReturnsAsync([]);

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1);

            Assert.NotNull(resultado);
            Assert.Equal(1, resultado!.RutaId);
            Assert.Null(resultado.ViajeId);
            Assert.Equal(4, resultado.CapacidadRestante);
            Assert.Equal(25, resultado.PesoRestanteKg);
            Assert.Empty(resultado.AvisosDeLaSalida);
        }

        [Fact]
        public async Task ListarAsync_ConViajeMaterializado_UsaElSnapshotDelViaje()
        {
            Ruta ruta = CrearRutaDelLunes();
            Viaje viaje = CrearViaje(id: 5, ruta: ruta, capacidad: 2, pesoMax: 5);
            _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);
            _viajes.Setup(v => v.ObtenerPorRutaYFechaAsync(1, SalidaDelLunes)).ReturnsAsync(viaje);
            _envios.Setup(e => e.ListarDisponiblesAsync(SalidaDelLunes, 1)).ReturnsAsync([]);

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1);

            Assert.NotNull(resultado);
            Assert.Equal(5, resultado!.ViajeId);
            Assert.Equal(2, resultado.CapacidadRestante);
            Assert.Equal(5, resultado.PesoRestanteKg);
        }

        [Fact]
        public async Task ListarAsync_ConRutaInactiva_DevuelveElAvisoSinBuscarEnvios()
        {
            Ruta ruta = CrearRutaDelLunes(estado: EstadoRuta.INACTIVA);
            _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);
            _viajes.Setup(v => v.ObtenerPorRutaYFechaAsync(1, SalidaDelLunes)).ReturnsAsync((Viaje?)null);

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1);

            Assert.NotNull(resultado);
            Assert.Contains(resultado!.AvisosDeLaSalida, a => a.Contains("inactiva"));
            _envios.Verify(
                e => e.ListarDisponiblesAsync(It.IsAny<DateTime>(), It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task ListarAsync_ConFechaEnUnDiaNoConfigurado_DevuelveNull()
        {
            Ruta ruta = CrearRutaDelLunes();
            _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1, new DateTime(2026, 11, 3));

            Assert.Null(resultado);
        }

        [Fact]
        public async Task ListarAsync_ConRutaDeOtroConductor_DevuelveNull()
        {
            Ruta ruta = CrearRutaDelLunes(usuarioId: 99);
            _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1);

            Assert.Null(resultado);
        }

        [Fact]
        public async Task AceptarAsync_SinViaje_CreaElViajeComoSnapshotYLaAsignacion()
        {
            Ruta ruta = CrearRutaDelLunes();
            Envio envio = CrearEnvio();
            Viaje? creado = null;

            _rutas.Setup(r => r.ObtenerPorIdParaModificarAsync(1)).ReturnsAsync(ruta);
            _viajes
                .Setup(v => v.ObtenerParaAsignarPorRutaYFechaAsync(1, SalidaDelLunes))
                .ReturnsAsync((Viaje?)null);
            _envios.Setup(e => e.ObtenerParaOportunidadAsync(10)).ReturnsAsync(envio);
            _viajes
                .Setup(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()))
                .Callback<Viaje>(v =>
                {
                    creado = v;
                    v.Id = 50;
                })
                .Returns(Task.CompletedTask);

            ResultadoOperacionOportunidad resultado = await _servicio.AceptarAsync(1, 10, 1);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(50, resultado.ViajeId);
            Assert.NotNull(creado);
            Assert.Equal(1, creado!.RutaId);
            Assert.Equal(ruta.DireccionOrigenId, creado.DireccionOrigenId);
            Assert.Equal(ruta.DireccionDestinoId, creado.DireccionDestinoId);
            Assert.Equal(SalidaDelLunes, creado.FechaSalida);
            Assert.Equal(SalidaDelLunes.AddMinutes(90), creado.FechaLlegada);
            Assert.Equal(25, creado.PesoMax);
            Assert.Equal(4, creado.CapacidadMaxPaquetes);
            Assert.Collection(
                creado.Paradas,
                p => Assert.Equal(1, p.Orden),
                p => Assert.Equal(2, p.Orden));
            Assert.Single(creado.Asignaciones);
            Assert.Equal(EstadoViaje.CONFIRMADO, creado.Asignaciones[0].Estado);
            Assert.Equal(EstadoEnvio.ConductorAsignado, envio.Estado);
            _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task AceptarAsync_ConViajeExistente_NoCreaOtroViaje()
        {
            Ruta ruta = CrearRutaDelLunes();
            Viaje viaje = CrearViaje(id: 5, ruta: ruta);
            Envio envio = CrearEnvio();

            _rutas.Setup(r => r.ObtenerPorIdParaModificarAsync(1)).ReturnsAsync(ruta);
            _viajes
                .Setup(v => v.ObtenerParaAsignarPorRutaYFechaAsync(1, SalidaDelLunes))
                .ReturnsAsync(viaje);
            _envios.Setup(e => e.ObtenerParaOportunidadAsync(10)).ReturnsAsync(envio);

            ResultadoOperacionOportunidad resultado = await _servicio.AceptarAsync(1, 10, 1);

            Assert.True(resultado.EsExitoso);
            Assert.Equal(5, resultado.ViajeId);
            Assert.Single(viaje.Asignaciones);
            _viajes.Verify(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()), Times.Never);
            _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task AceptarAsync_ConRutaDeOtroConductor_DevuelveNoEncontrado()
        {
            Ruta ruta = CrearRutaDelLunes(usuarioId: 99);
            _rutas.Setup(r => r.ObtenerPorIdParaModificarAsync(1)).ReturnsAsync(ruta);

            ResultadoOperacionOportunidad resultado = await _servicio.AceptarAsync(1, 10, 1);

            Assert.True(resultado.NoEncontrado);
            _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
        }

        private static Ruta CrearRutaDelLunes(
            int usuarioId = 1,
            EstadoRuta estado = EstadoRuta.ACTIVA)
            => RutaTestData.CrearRuta(
                id: 1,
                usuarioId: usuarioId,
                estado: estado,
                fechaInicio: new DateTime(2026, 11, 1),
                horaSalida: new TimeOnly(8, 0),
                horaLlegada: new TimeOnly(9, 30),
                dias: [DayOfWeek.Monday],
                paradas:
                [
                    RutaTestData.CrearParada(1, "Ciudadela", 1),
                    RutaTestData.CrearParada(2, "Liniers", 2)
                ]);

        private static Viaje CrearViaje(int id, Ruta ruta, int capacidad = 4, double pesoMax = 25) => new()
        {
            Id = id,
            RutaId = ruta.Id,
            Ruta = ruta,
            DireccionOrigenId = ruta.DireccionOrigenId,
            DireccionOrigen = ruta.DireccionOrigen,
            DireccionDestinoId = ruta.DireccionDestinoId,
            DireccionDestino = ruta.DireccionDestino,
            PesoMax = pesoMax,
            CapacidadMaxPaquetes = capacidad,
            FechaSalida = SalidaDelLunes,
            FechaLlegada = SalidaDelLunes.AddMinutes(90),
            Estado = EstadoActividadViaje.ACTIVO
        };

        private static Envio CrearEnvio() => new()
        {
            Id = 10,
            UsuarioId = 2,
            Estado = EstadoEnvio.BuscandoConductor,
            FechaDesde = new DateTime(2026, 11, 1),
            FechaHasta = new DateTime(2026, 11, 5),
            Monto = 5000m,
            Paquete = new Paquete
            {
                Descripcion = "Caja",
                PesoKg = 3,
                Tamanio = TamanioEnvio.Mediano
            },
            DireccionRetiroId = 300,
            DireccionRetiro = RutaTestData.CrearDireccion(300, "Retiro"),
            DireccionEntregaId = 301,
            DireccionEntrega = RutaTestData.CrearDireccion(301, "Entrega")
        };
    }
}
