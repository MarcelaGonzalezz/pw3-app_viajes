using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios
{
    public class MaterializadorDeViajesTests
    {
        // lunes 2/11/2026 08:30 en Argentina (UTC-3)
        private static readonly DateTimeOffset Ahora = new(2026, 11, 2, 11, 30, 0, TimeSpan.Zero);
        private static readonly DateTime Salida = new(2026, 11, 2, 8, 0, 0);

        private readonly Mock<IRutaRepositorio> _rutas = new();
        private readonly Mock<IViajeRepositorio> _viajes = new();
        private readonly MaterializadorDeViajes _materializador;

        public MaterializadorDeViajesTests()
        {
            _materializador = new MaterializadorDeViajes(
                _rutas.Object,
                _viajes.Object,
                new RelojFijo(Ahora));
        }

        [Fact]
        public async Task MaterializarSalidasAsync_CuandoLlegoLaHoraYCreoElViaje()
        {
            Ruta ruta = RutaTestData.CrearRuta(
                fechaInicio: new DateTime(2026, 11, 1),
                horaSalida: new TimeOnly(8, 0),
                horaLlegada: new TimeOnly(9, 30),
                dias: [DayOfWeek.Monday],
                paradas:
                [
                    RutaTestData.CrearParada(1, "Ciudadela", 1),
                    RutaTestData.CrearParada(2, "Liniers", 2)
                ]);

            Viaje? creado = null;

            _rutas.Setup(r => r.ObtenerRutasActivasAsync()).ReturnsAsync([ruta]);
            _viajes
                .Setup(v => v.ObtenerParaAsignarPorRutaYFechaAsync(ruta.Id, Salida))
                .ReturnsAsync((Viaje?)null);
            _viajes
                .Setup(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()))
                .Callback<Viaje>(v => creado = v)
                .Returns(Task.CompletedTask);

            int creados = await _materializador.MaterializarSalidasAsync();

            Assert.Equal(1, creados);
            Assert.NotNull(creado);
            Assert.Equal(Salida, creado!.FechaSalida);
            Assert.Equal(Salida.AddMinutes(90), creado.FechaLlegada);
            Assert.Equal(2, creado.Paradas.Count);
            _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task MaterializarSalidasAsync_CuandoElViajeYaExiste_NoLoDuplica()
        {
            Ruta ruta = RutaTestData.CrearRuta(
                fechaInicio: new DateTime(2026, 11, 1),
                horaSalida: new TimeOnly(8, 0),
                horaLlegada: new TimeOnly(9, 30),
                dias: [DayOfWeek.Monday]);

            _rutas.Setup(r => r.ObtenerRutasActivasAsync()).ReturnsAsync([ruta]);
            _viajes
                .Setup(v => v.ObtenerParaAsignarPorRutaYFechaAsync(ruta.Id, Salida))
                .ReturnsAsync(new Viaje { Id = 5, RutaId = ruta.Id, FechaSalida = Salida });

            int creados = await _materializador.MaterializarSalidasAsync();

            Assert.Equal(0, creados);
            _viajes.Verify(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()), Times.Never);
            _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Never);
        }

        [Fact]
        public async Task MaterializarSalidasAsync_CuandoHoyNoEsDiaDeLaRuta_NoCreaNada()
        {
            Ruta ruta = RutaTestData.CrearRuta(
                fechaInicio: new DateTime(2026, 11, 1),
                horaSalida: new TimeOnly(8, 0),
                horaLlegada: new TimeOnly(9, 30),
                dias: [DayOfWeek.Tuesday]);

            _rutas.Setup(r => r.ObtenerRutasActivasAsync()).ReturnsAsync([ruta]);

            int creados = await _materializador.MaterializarSalidasAsync();

            Assert.Equal(0, creados);
            _viajes.Verify(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()), Times.Never);
        }
    }
}
