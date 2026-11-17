using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.Viajes;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Viajes
{
    public class VencedorDeViajesYRutasTests
    {
        // lunes 2/11/2026 a las 10:00
        private static readonly DateTime Ahora = new(2026, 11, 2, 10, 0, 0);

        private readonly Mock<IViajeRepositorio> _viajes = new();
        private readonly Mock<IRutaRepositorio> _rutas = new();
        private readonly VencedorDeViajesYRutas _vencedor;

        public VencedorDeViajesYRutasTests()
        {
            _vencedor = new VencedorDeViajesYRutas(_viajes.Object, _rutas.Object);
            _viajes.Setup(v => v.ObtenerActivosQueYaSalieronAsync(Ahora)).ReturnsAsync([]);
            _rutas.Setup(r => r.ObtenerActivasConFechaFinHastaAsync(Ahora.Date)).ReturnsAsync([]);
        }

        [Fact]
        public async Task VencerAsync_PasaAInactivoSoloLosViajesQueYaLlegaron()
        {
            // salio 08:00 y llego 09:30 -> vencido
            Viaje llego = ViajeTestData.CrearViaje(id: 1, fechaSalida: new DateTime(2026, 11, 2, 8, 0, 0));
            // salio 09:00 y llega 10:30 -> todavia no
            Viaje enCamino = ViajeTestData.CrearViaje(id: 2, fechaSalida: new DateTime(2026, 11, 2, 9, 0, 0));

            _viajes.Setup(v => v.ObtenerActivosQueYaSalieronAsync(Ahora)).ReturnsAsync([llego, enCamino]);

            await _vencedor.VencerAsync(Ahora);

            Assert.Equal(EstadoActividadViaje.INACTIVO, llego.Estado);
            Assert.Equal(EstadoActividadViaje.ACTIVO, enCamino.Estado);
            _viajes.Verify(v => v.GuardarCambiosAsync(), Times.Once);
        }

        [Fact]
        public async Task VencerAsync_CuandoElViajeNoTieneLlegada_UsaLaHoraDeSalida()
        {
            Viaje sinLlegada = ViajeTestData.CrearViaje(id: 1, fechaSalida: new DateTime(2026, 11, 2, 9, 0, 0));
            sinLlegada.FechaLlegada = null;

            _viajes.Setup(v => v.ObtenerActivosQueYaSalieronAsync(Ahora)).ReturnsAsync([sinLlegada]);

            await _vencedor.VencerAsync(Ahora);

            Assert.Equal(EstadoActividadViaje.INACTIVO, sinLlegada.Estado);
        }

        [Fact]
        public async Task VencerAsync_PasaAInactivaSoloLasRutasCuyaFechaHastaYaPaso()
        {
            // termino ayer -> vencida
            Ruta ayer = RutaTestData.CrearRuta(id: 1, fechaFin: new DateTime(2026, 11, 1), horaLlegada: new TimeOnly(18, 0));
            // termina hoy y llego a las 09:30 -> vencida
            Ruta hoyYaLlego = RutaTestData.CrearRuta(id: 2, fechaFin: new DateTime(2026, 11, 2), horaLlegada: new TimeOnly(9, 30));
            // termina hoy pero llega a las 18:00 -> todavia no
            Ruta hoyFalta = RutaTestData.CrearRuta(id: 3, fechaFin: new DateTime(2026, 11, 2), horaLlegada: new TimeOnly(18, 0));

            _rutas.Setup(r => r.ObtenerActivasConFechaFinHastaAsync(Ahora.Date))
                .ReturnsAsync([ayer, hoyYaLlego, hoyFalta]);

            await _vencedor.VencerAsync(Ahora);

            Assert.Equal(EstadoRuta.INACTIVA, ayer.Estado);
            Assert.Equal(EstadoRuta.INACTIVA, hoyYaLlego.Estado);
            Assert.Equal(EstadoRuta.ACTIVA, hoyFalta.Estado);
            _rutas.Verify(r => r.GuardarCambiosAsync(), Times.Once);
        }
    }
}
