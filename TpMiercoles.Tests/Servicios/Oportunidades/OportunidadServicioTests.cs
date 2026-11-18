using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Oportunidades
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
        public async Task ListarAsync_SinDesvioMaximo_DescartaEnviosQueDesvianMasDelDefault()
        {
            PrepararSalidaConUnEnvio();
            ConDistanciaConEnvio(123); // habitual 10 km --> desvio 113 km

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1);

            Assert.NotNull(resultado);
            Assert.Empty(resultado!.Oportunidades);
        }

        [Fact]
        public async Task ListarAsync_SinDesvioMaximo_IncluyeEnviosDentroDelDefault()
        {
            PrepararSalidaConUnEnvio();
            ConDistanciaConEnvio(15); // desvio 5 km

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1);

            Assert.NotNull(resultado);
            OportunidadDTO oportunidad = Assert.Single(resultado!.Oportunidades);
            Assert.Equal(5, oportunidad.DesvioKm);
        }

        [Fact]
        public async Task ListarAsync_ConDesvioMaximoExplicito_IncluyeEnviosQueSuperanElDefault()
        {
            PrepararSalidaConUnEnvio();
            ConDistanciaConEnvio(123); // desvio 113 km

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1, desvioMaximoKm: 150);

            Assert.NotNull(resultado);
            OportunidadDTO oportunidad = Assert.Single(resultado!.Oportunidades);
            Assert.Equal(113, oportunidad.DesvioKm);
        }

        [Fact]
        public async Task ListarAsync_ConDesvioIgualAlMaximo_LoIncluye()
        {
            PrepararSalidaConUnEnvio();
            ConDistanciaConEnvio(20); // desvio exactamente 10 km (el filtro es '>' estricto)

            OportunidadesDeRutaDTO? resultado = await _servicio.ListarAsync(1, 1);

            Assert.NotNull(resultado);
            Assert.Single(resultado!.Oportunidades);
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

        // ---------- Busqueda en dos pasos (estimador + rutas reales) ----------

        [Fact]
        public async Task ListarAsync_DosPasos_ElMargenNoPierdeUnEnvioQueConRutasRealesCumple()
        {
            PrepararSalidaConUnEnvio();
            var estimador = RutasQueResponden<IEstimadorDeRutas>(kmConEnvio: 21, conTrazado: false); // 11 km: entra en el margen (12)
            var reales = RutasQueResponden<IServicioRutas>(kmConEnvio: 19, conTrazado: true);       // 9 km

            OportunidadesDeRutaDTO? resultado = await ServicioEnDosPasos(reales, estimador).ListarAsync(1, 1);

            OportunidadDTO oportunidad = Assert.Single(resultado!.Oportunidades);
            Assert.Equal(9, oportunidad.DesvioKm);
            Assert.False(resultado.DistanciasEstimadas);
        }

        [Fact]
        public async Task ListarAsync_DosPasos_SiLaConfirmacionVieneSinTrazado_TodoQuedaEstimado()
        {
            PrepararSalidaConUnEnvio();
            var estimador = RutasQueResponden<IEstimadorDeRutas>(kmConEnvio: 15, conTrazado: false);
            var reales = RutasQueResponden<IServicioRutas>(kmConEnvio: 18, conTrazado: false); // el plan B respondio con el mock

            OportunidadesDeRutaDTO? resultado = await ServicioEnDosPasos(reales, estimador).ListarAsync(1, 1);

            OportunidadDTO oportunidad = Assert.Single(resultado!.Oportunidades);
            Assert.Equal(5, oportunidad.DesvioKm); // valor del paso 1
            Assert.True(resultado.DistanciasEstimadas);
        }

        [Fact]
        public async Task ListarAsync_DosPasos_SoloConfirmaLosCincoMejores()
        {
            Ruta ruta = CrearRutaDelLunes();
            _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);
            _viajes.Setup(v => v.ObtenerPorRutaYFechaAsync(1, SalidaDelLunes)).ReturnsAsync((Viaje?)null);
            List<Envio> envios = Enumerable.Range(1, 7).Select(i => CrearEnvio(10 + i, 1000m * i)).ToList();
            _envios.Setup(e => e.ListarDisponiblesAsync(SalidaDelLunes, 1)).ReturnsAsync(envios);
            var estimador = RutasQueResponden<IEstimadorDeRutas>(kmConEnvio: 15, conTrazado: false);
            var reales = RutasQueResponden<IServicioRutas>(kmConEnvio: 13, conTrazado: true);

            OportunidadesDeRutaDTO? resultado = await ServicioEnDosPasos(reales, estimador).ListarAsync(1, 1);

            Assert.Equal(new[] { 17, 16, 15, 14, 13 }, resultado!.Oportunidades.Select(o => o.EnvioId));
            Assert.False(resultado.DistanciasEstimadas);
            // recorrido habitual + 5 finalistas
            reales.Verify(s => s.CalcularRutaAsync(
                It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
                It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()), Times.Exactly(6));
        }

        [Fact]
        public async Task ListarAsync_DosPasos_EnvioSinCaminoPosible_SeDescarta()
        {
            PrepararSalidaConUnEnvio();
            var estimador = RutasQueResponden<IEstimadorDeRutas>(kmConEnvio: 15, conTrazado: false);
            var reales = RutasQueResponden<IServicioRutas>(kmConEnvio: 15, conTrazado: true);
            ConError(reales, 404);

            OportunidadesDeRutaDTO? resultado = await ServicioEnDosPasos(reales, estimador).ListarAsync(1, 1);

            Assert.Empty(resultado!.Oportunidades);
            Assert.False(resultado.DistanciasEstimadas);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(500)]
        public async Task ListarAsync_DosPasos_SiOrsFallaPorOtroMotivo_UsaElPaso1Completo(int codigo)
        {
            PrepararSalidaConUnEnvio();
            var estimador = RutasQueResponden<IEstimadorDeRutas>(kmConEnvio: 15, conTrazado: false);
            var reales = RutasQueResponden<IServicioRutas>(kmConEnvio: 18, conTrazado: true);
            ConError(reales, codigo);

            OportunidadesDeRutaDTO? resultado = await ServicioEnDosPasos(reales, estimador).ListarAsync(1, 1);

            OportunidadDTO oportunidad = Assert.Single(resultado!.Oportunidades);
            Assert.Equal(5, oportunidad.DesvioKm); // valor del paso 1
            Assert.True(resultado.DistanciasEstimadas);
        }

        // la ruta con el envio responde con ese error
        private static void ConError(Mock<IServicioRutas> rutas, int codigo)
        {
            rutas.Setup(s => s.CalcularRutaAsync(
                    It.IsAny<Coordenada>(),
                    It.IsAny<Coordenada>(),
                    It.Is<IReadOnlyList<Coordenada>?>(p => p != null && p.Count > 2),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RutaNoEncontradaException($"ORS respondió {codigo}.") { CodigoHttp = codigo });
        }

        private OportunidadServicio ServicioEnDosPasos(Mock<IServicioRutas> reales, Mock<IEstimadorDeRutas> estimador)
            => new(_rutas.Object, _viajes.Object, _envios.Object, reales.Object, new RelojFijo(Ahora), estimador.Object);

        // habitual 10 km; con el envio (mas de 2 paradas) los km indicados
        private static Mock<T> RutasQueResponden<T>(double kmConEnvio, bool conTrazado) where T : class, IServicioRutas
        {
            string? trazado = conTrazado ? "{}" : null;
            var rutas = new Mock<T>();

            rutas.Setup(s => s.CalcularRutaAsync(
                    It.IsAny<Coordenada>(),
                    It.IsAny<Coordenada>(),
                    It.Is<IReadOnlyList<Coordenada>?>(p => p == null || p.Count <= 2),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultadoRuta(10_000, 900, trazado));

            rutas.Setup(s => s.CalcularRutaAsync(
                    It.IsAny<Coordenada>(),
                    It.IsAny<Coordenada>(),
                    It.Is<IReadOnlyList<Coordenada>?>(p => p != null && p.Count > 2),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultadoRuta(kmConEnvio * 1000, 900, trazado));

            return rutas;
        }

        private static Envio CrearEnvio(int id, decimal monto)
        {
            Envio envio = CrearEnvio();
            envio.Id = id;
            envio.Monto = monto;
            return envio;
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

        private void PrepararSalidaConUnEnvio()
        {
            Ruta ruta = CrearRutaDelLunes();
            _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);
            _viajes.Setup(v => v.ObtenerPorRutaYFechaAsync(1, SalidaDelLunes)).ReturnsAsync((Viaje?)null);
            _envios.Setup(e => e.ListarDisponiblesAsync(SalidaDelLunes, 1)).ReturnsAsync([CrearEnvio()]);
        }

        // la ruta habitual tiene 2 paradas; con el envio (retiro + entrega) pasan a ser 4
        private void ConDistanciaConEnvio(double km)
        {
            _servicioRutas
                .Setup(s => s.CalcularRutaAsync(
                    It.IsAny<Coordenada>(),
                    It.IsAny<Coordenada>(),
                    It.Is<IReadOnlyList<Coordenada>?>(p => p != null && p.Count > 2),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResultadoRuta(km * 1000, 900));
        }
    }
}
