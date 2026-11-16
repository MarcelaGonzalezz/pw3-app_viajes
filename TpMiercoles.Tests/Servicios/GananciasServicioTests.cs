using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Servicios.DTOs.Ganancias;
using TpMiercoles.Servicios.DTOs.Historial;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios
{
    public class GananciasServicioTests
    {
        private const int ConductorId = 1;

        private static readonly DateTimeOffset PrimeroDeNoviembre =
            new(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(-3));

        private readonly Mock<IRutaRepositorio> _rutas = new();
        private readonly Mock<IOptimizadorServicio> _optimizador = new();
        private readonly Mock<IHistorialServicio> _historial = new();
        private readonly Mock<IEnvioRepositorio> _envios = new();
        private readonly GananciasServicio _servicio;

        public GananciasServicioTests()
        {
            UsarRutas(RutaTestData.CrearRuta(dias: [DayOfWeek.Monday]));

            _historial
                .Setup(h => h.ObtenerAsync(ConductorId, null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new HistorialDTO());

            _envios
                .Setup(e => e.ListarPorIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
                .ReturnsAsync((IReadOnlyCollection<int> ids) => ids.Select(CrearEnvio).ToList());

            _servicio = new GananciasServicio(
                _rutas.Object,
                _optimizador.Object,
                _historial.Object,
                _envios.Object,
                new RelojFijo(PrimeroDeNoviembre));
        }

        [Fact]
        public async Task UnEnvioSeUsaEnUnaSolaSalida()
        {
            List<SolicitudOptimizacionDTO> recibidas =
                ConfigurarOptimizador(0m, (1, 5000m), (2, 3000m), (3, 1000m));

            GananciasMensualDTO dto = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MayorGanancia);

            List<int> usados = dto.Salidas.SelectMany(s => s.EnviosIds).ToList();

            Assert.Equal([1, 2, 3], usados);
            Assert.Equal(9000m, dto.GananciaPotencial);
            Assert.Equal(5, dto.ViajesAnalizados);
            Assert.Equal(2, dto.SalidasSinEnviosCompatibles);
            Assert.Equal([1, 2, 3], recibidas[3].EnviosExcluidos!.Order());
        }

        [Fact]
        public async Task ElDetalleYLasSemanasSumanElTotal()
        {
            ConfigurarOptimizador(0m, (1, 5000m), (2, 3000m), (3, 1000m));

            GananciasMensualDTO dto = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MayorGanancia);

            Assert.Equal(dto.GananciaPotencial, dto.Salidas.Sum(s => s.GananciaPotencial));
            Assert.Equal(dto.GananciaPotencial, dto.Semanas.Sum(s => s.GananciaPotencial));
            Assert.Equal(dto.ViajesAnalizados, dto.Semanas.Sum(s => s.Viajes));
            Assert.Equal(dto.PaquetesPosibles, dto.Semanas.Sum(s => s.Paquetes));

            Assert.Equal(6, dto.Semanas.Count);
            Assert.Equal(new DateTime(2026, 11, 1), dto.Semanas[0].Desde);
            Assert.Equal(0, dto.Semanas[0].Viajes);
            Assert.Equal(new DateTime(2026, 11, 30), dto.Semanas[^1].Desde);
            Assert.Equal(1, dto.Semanas[^1].Viajes);
        }

        [Fact]
        public async Task CambiarElCriterioCambiaLaPropuestaElegida()
        {
            UsarRutas(RutaTestData.CrearRuta(
                dias: [DayOfWeek.Monday],
                fechaFin: new DateTime(2026, 11, 2)));
            ConfigurarOptimizador(0m, (1, 5000m), (2, 1000m));

            GananciasMensualDTO porGanancia = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MayorGanancia);
            GananciasMensualDTO porDesvio = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MenorDesvio);

            Assert.Equal(5000m, porGanancia.GananciaPotencial);
            Assert.Equal([1], porGanancia.Salidas[0].EnviosIds);
            Assert.Equal("Mayor ganancia", porGanancia.Salidas[0].PropuestaElegida);

            Assert.Equal(1000m, porDesvio.GananciaPotencial);
            Assert.Equal([2], porDesvio.Salidas[0].EnviosIds);
            Assert.Equal("Menor desvío", porDesvio.Salidas[0].PropuestaElegida);
        }

        [Fact]
        public async Task UnaSalidaDescartadaNoRompeElMes()
        {
            DateTime rechazada = new(2026, 11, 9, 8, 0, 0);
            (int Id, decimal Monto)[] envios = [(1, 5000m), (2, 3000m)];

            _optimizador
                .Setup(o => o.CalcularAsync(It.IsAny<SolicitudOptimizacionDTO>(), ConductorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SolicitudOptimizacionDTO s, int _, CancellationToken _) =>
                    s.FechaSalida == rechazada
                        ? new ResultadoOptimizacionDTO { Estado = EstadoResultadoOptimizacion.SalidaNoHabilitada }
                        : Responder(s, envios, 0m));

            GananciasMensualDTO dto = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MayorGanancia);

            Assert.Equal(1, dto.SalidasDescartadas);
            Assert.Equal(4, dto.ViajesAnalizados);
            Assert.DoesNotContain(dto.Salidas, s => s.FechaSalida == rechazada);
            Assert.Equal(8000m, dto.GananciaPotencial);
        }

        [Fact]
        public async Task LasTresGananciasSeInformanPorSeparado()
        {
            _historial
                .Setup(h => h.ObtenerAsync(ConductorId, null, null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(new HistorialDTO { GananciaEfectiva = 44000m, EnviosTransportados = 6 });
            ConfigurarOptimizador(2000m, (1, 5000m));

            GananciasMensualDTO dto = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MayorGanancia);

            Assert.Equal(44000m, dto.GananciaEfectiva);
            Assert.Equal(6, dto.EnviosEntregados);
            Assert.Equal(10000m, dto.GananciaComprometida);
            Assert.Equal(5000m, dto.GananciaPotencial);
        }

        [Fact]
        public async Task ElLimiteDeDesvioLlegaAlOptimizadorSoloSiSePide()
        {
            List<SolicitudOptimizacionDTO> recibidas = ConfigurarOptimizador(0m, (1, 5000m));

            GananciasMensualDTO sinLimite = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MayorGanancia);
            GananciasMensualDTO conLimite = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MayorGanancia, 5);

            Assert.Null(sinLimite.DesvioMaximoKm);
            Assert.Equal(5, conLimite.DesvioMaximoKm);

            Assert.Equal(10, recibidas.Count);
            Assert.All(recibidas.Take(5), s => Assert.Equal(double.MaxValue, s.DesvioMaximoKm));
            Assert.All(recibidas.Skip(5), s => Assert.Equal(5, s.DesvioMaximoKm));
        }

        [Fact]
        public async Task CadaSalidaInformaQuePaquetesHayQueAceptar()
        {
            ConfigurarOptimizador(0m, (1, 5000m), (2, 3000m));

            GananciasMensualDTO dto = await _servicio.CalcularAsync(
                ConductorId, 2026, 11, CriterioOptimizacion.MayorGanancia);

            PaquetePropuestoDTO paquete = Assert.Single(dto.Salidas[0].Paquetes);
            Assert.Equal(1, paquete.EnvioId);
            Assert.Equal("Paquete 1", paquete.Descripcion);
            Assert.Equal("Retiro 1", paquete.Retiro);
            Assert.Equal("Entrega 1", paquete.Entrega);

            Assert.Equal(2, Assert.Single(dto.Salidas[1].Paquetes).EnvioId);
            Assert.Empty(dto.Salidas[2].Paquetes);

            // Una sola consulta para todo el mes, no una por salida.
            _envios.Verify(e => e.ListarPorIdsAsync(It.IsAny<IReadOnlyCollection<int>>()), Times.Once);
        }

        private static Envio CrearEnvio(int id) => new()
        {
            Id = id,
            Monto = 1000m * id,
            Paquete = new Paquete { Descripcion = $"Paquete {id}", PesoKg = 2 },
            DireccionRetiro = new Direccion { Texto = $"Retiro {id}" },
            DireccionEntrega = new Direccion { Texto = $"Entrega {id}" }
        };

        private void UsarRutas(params Ruta[] rutas)
        {
            _rutas
                .Setup(r => r.ObtenerRutasActivasDeUnUsuarioAsync(ConductorId))
                .ReturnsAsync(rutas.ToList());
        }

        private List<SolicitudOptimizacionDTO> ConfigurarOptimizador(
            decimal comprometido,
            params (int Id, decimal Monto)[] envios)
        {
            List<SolicitudOptimizacionDTO> recibidas = [];

            _optimizador
                .Setup(o => o.CalcularAsync(It.IsAny<SolicitudOptimizacionDTO>(), ConductorId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SolicitudOptimizacionDTO s, int _, CancellationToken _) =>
                {
                    recibidas.Add(s);
                    return Responder(s, envios, comprometido);
                });

            return recibidas;
        }

        private static ResultadoOptimizacionDTO Responder(
            SolicitudOptimizacionDTO solicitud,
            (int Id, decimal Monto)[] envios,
            decimal comprometido)
        {
            List<(int Id, decimal Monto)> disponibles = envios
                .Where(e => solicitud.EnviosExcluidos?.Contains(e.Id) != true)
                .ToList();

            if (disponibles.Count == 0)
            {
                return new ResultadoOptimizacionDTO
                {
                    Estado = EstadoResultadoOptimizacion.Correcto,
                    IngresoComprometido = comprometido
                };
            }

            (int Id, decimal Monto) mayor = disponibles.MaxBy(e => e.Monto);
            (int Id, decimal Monto) menor = disponibles.MinBy(e => e.Monto);

            List<PropuestaOptimizacionDTO> propuestas = mayor.Id == menor.Id
                ? [Propuesta(mayor, comprometido, "Mayor ganancia", "Menor desvío", "Equilibrada")]
                : [Propuesta(mayor, comprometido, "Mayor ganancia", "Equilibrada"),
                   Propuesta(menor, comprometido, "Menor desvío")];

            return new ResultadoOptimizacionDTO
            {
                Estado = EstadoResultadoOptimizacion.Correcto,
                Propuestas = propuestas,
                IngresoComprometido = comprometido
            };
        }

        private static PropuestaOptimizacionDTO Propuesta(
            (int Id, decimal Monto) envio,
            decimal comprometido,
            params string[] etiquetas) => new()
            {
                Etiquetas = [.. etiquetas],
                EnviosNuevosIds = [envio.Id],
                IngresoAdicional = envio.Monto,
                IngresoTotal = envio.Monto + comprometido,
                PesoAdicionalKg = 2,
                PesoTotalKg = 2,
                CantidadTotalPaquetes = 1,
                DesvioTotalKm = 1,
                DistanciaTotalKm = 10,
                DuracionTotalMinutos = 20
            };
    }
}