using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class ResolvedorSalidaOptimizacionTests
{
    private readonly Mock<IRutaRepositorio> _rutas = new();
    private readonly Mock<IViajeRepositorio> _viajes = new();
    // Lunes 02/11/2026, 07:00 Argentina.
    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero));
    private ResolvedorSalidaOptimizacion Crear() => new(_viajes.Object, _reloj, _rutas.Object);

    [Theory]
    [InlineData(10, 8)]
    [InlineData(11, 8)]
    [InlineData(12, 9)]
    public async Task Viaje_ResuelveReferenciaHorariaSinCambiarSalida(int horaUtc, int horaEvaluacion)
    {
        _reloj.Fijar(new DateTimeOffset(2026, 11, 2, horaUtc, 0, 0, TimeSpan.Zero));
        var viaje = OptimizacionTestData.Viaje(OptimizacionTestData.Ruta());
        _viajes.Setup(r => r.ObtenerDetalleAsync(20)).ReturnsAsync(viaje);
        var salida = await Crear().ResolverViajeAsync(1, 20, 7);
        Assert.NotNull(salida);
        Assert.Equal(OptimizacionTestData.Fecha, salida.FechaSalidaProgramada);
        Assert.Equal(OptimizacionTestData.Fecha.Date.AddHours(horaEvaluacion), salida.MomentoEvaluacion);
        Assert.Equal(OptimizacionTestData.Fecha, viaje.FechaSalida);
    }

    [Theory]
    [InlineData(2, 7)]
    [InlineData(1, 99)]
    public async Task Viaje_DeOtraRutaOConductor_NoSeExpone(int rutaId, int usuarioId)
    {
        _viajes.Setup(r => r.ObtenerDetalleAsync(20)).ReturnsAsync(OptimizacionTestData.Viaje(OptimizacionTestData.Ruta()));
        Assert.Null(await Crear().ResolverViajeAsync(rutaId, 20, usuarioId));
    }

    [Fact]
    public async Task ViajeInexistente_DevuelveNull()
        => Assert.Null(await Crear().ResolverViajeAsync(1, 999, 7));

    [Fact]
    public async Task Proxima_SinViajeMaterializado_UsaLaPlantilla()
    {
        _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(OptimizacionTestData.Ruta());
        var salida = await Crear().ResolverProximaSalidaAsync(1, 7);
        Assert.NotNull(salida);
        Assert.Null(salida.ViajeId);
        Assert.Equal(OptimizacionTestData.Fecha, salida.FechaSalidaProgramada);
        Assert.Equal(10, salida.PesoMaximoKg);
        _viajes.Verify(v => v.AgregarSinGuardarAsync(It.IsAny<Viaje>()), Times.Never);
    }

    [Fact]
    public async Task Proxima_YaMaterializada_RecuperaDetalleYCompromisos()
    {
        var ruta = OptimizacionTestData.Ruta();
        var viaje = OptimizacionTestData.Viaje(ruta);
        viaje.PesoMax = 6;
        OptimizacionTestData.Asignar(viaje, OptimizacionTestData.Envio());
        _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);
        _viajes.Setup(r => r.ObtenerPorRutaYFechaAsync(1, OptimizacionTestData.Fecha)).ReturnsAsync(viaje);
        _viajes.Setup(r => r.ObtenerDetalleAsync(20)).ReturnsAsync(viaje);
        var salida = await Crear().ResolverProximaSalidaAsync(1, 7);
        Assert.NotNull(salida);
        Assert.Equal(20, salida.ViajeId);
        Assert.Equal(6, salida.PesoMaximoKg);
        Assert.Single(salida.Asignaciones);
    }

    [Fact]
    public async Task Proxima_SiYaLlegoLaHora_EligeElSiguienteDiaProgramado()
    {
        _reloj.Fijar(new DateTimeOffset(2026, 11, 2, 11, 0, 0, TimeSpan.Zero));
        _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(OptimizacionTestData.Ruta());
        var salida = await Crear().ResolverProximaSalidaAsync(1, 7);
        Assert.Equal(new DateTime(2026, 11, 9, 8, 0, 0), salida!.FechaSalidaProgramada);
    }

    [Fact]
    public async Task Proxima_FueraDeVigencia_NoOfreceSalida()
    {
        var ruta = OptimizacionTestData.Ruta();
        ruta.FechaFin = new DateTime(2026, 11, 1);
        _rutas.Setup(r => r.ObtenerPorIdAsync(1)).ReturnsAsync(ruta);
        Assert.Null(await Crear().ResolverProximaSalidaAsync(1, 7));
    }

    [Fact]
    public async Task Listar_CombinaExistentesYProximaSinDuplicarLaMaterializada()
    {
        var ruta = OptimizacionTestData.Ruta();
        var proximo = OptimizacionTestData.Viaje(ruta);
        var pasado = OptimizacionTestData.Viaje(ruta);
        pasado.Id = 19;
        pasado.FechaSalida = OptimizacionTestData.Fecha.AddDays(-7);
        var rutaDos = OptimizacionTestData.Ruta();
        rutaDos.Id = 2;
        _rutas.Setup(r => r.ObtenerRutasActivasDeUnUsuarioAsync(7)).ReturnsAsync([ruta, rutaDos]);
        _viajes.Setup(r => r.ListarPorUsuarioAsync(7, EstadoActividadViaje.ACTIVO)).ReturnsAsync([proximo, pasado]);
        var salidas = await Crear().ListarSalidasAsync(7);
        Assert.Equal(3, salidas.Count);
        Assert.Equal(19, salidas[0].ViajeId);
        Assert.Single(salidas, s => s.RutaId == 1 && s.FechaSalida == OptimizacionTestData.Fecha);
        Assert.Null(Assert.Single(salidas, s => s.RutaId == 2).ViajeId);
    }

    [Fact]
    public async Task Listar_NoPresentaViajeInactivoComoSalidaSinMaterializar()
    {
        var ruta = OptimizacionTestData.Ruta();
        var viaje = OptimizacionTestData.Viaje(ruta);
        viaje.Estado = EstadoActividadViaje.INACTIVO;
        _rutas.Setup(r => r.ObtenerRutasActivasDeUnUsuarioAsync(7)).ReturnsAsync([ruta]);
        _viajes.Setup(r => r.ListarPorUsuarioAsync(7, EstadoActividadViaje.ACTIVO)).ReturnsAsync([]);
        _viajes.Setup(r => r.ObtenerPorRutaYFechaAsync(1, OptimizacionTestData.Fecha)).ReturnsAsync(viaje);
        Assert.Empty(await Crear().ListarSalidasAsync(7));
    }

    [Fact]
    public async Task Listar_NoIncluyeViajesDeRutasInactivas()
    {
        var ruta = OptimizacionTestData.Ruta();
        ruta.Estado = EstadoRuta.INACTIVA;
        _rutas.Setup(r => r.ObtenerRutasActivasDeUnUsuarioAsync(7)).ReturnsAsync([]);
        _viajes.Setup(r => r.ListarPorUsuarioAsync(7, EstadoActividadViaje.ACTIVO))
            .ReturnsAsync([OptimizacionTestData.Viaje(ruta)]);
        Assert.Empty(await Crear().ListarSalidasAsync(7));
    }

    [Fact]
    public async Task Listar_Cancelacion_NoConsultaRepositorios()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(() => Crear().ListarSalidasAsync(7, new CancellationToken(true)));
        _rutas.VerifyNoOtherCalls();
        _viajes.VerifyNoOtherCalls();
    }
}
