using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs.Optimizacion;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class ReglasOptimizacionTests
{
    [Theory]
    [InlineData(7, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 9)]
    public void MomentoEvaluacion_UsaSalidaFuturaOAhora(int horaActual, int horaEsperada)
    {
        var fecha = OptimizacionTestData.Fecha;
        Assert.Equal(fecha.Date.AddHours(horaEsperada),
            ReferenciaTemporalOptimizacion.DeterminarMomentoEvaluacion(fecha, fecha.Date.AddHours(horaActual)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Solicitud_RechazaDesvioInvalido(double desvio)
    {
        Assert.NotEmpty(new ValidadorSolicitudOptimizacion().Validar(
            new SolicitudOptimizacionDTO { RutaId = 1, DesvioMaximoKm = desvio }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(20)]
    public void Solicitud_AdmiteCeroDesvioYViajeOpcional(int? viajeId)
    {
        Assert.Empty(new ValidadorSolicitudOptimizacion().Validar(
            new SolicitudOptimizacionDTO { RutaId = 1, ViajeId = viajeId, DesvioMaximoKm = 0 }));
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Solicitud_RechazaIdentificadoresInvalidos(int rutaId, int? viajeId)
        => Assert.NotEmpty(new ValidadorSolicitudOptimizacion().Validar(
            new SolicitudOptimizacionDTO { RutaId = rutaId, ViajeId = viajeId, DesvioMaximoKm = 5 }));

    [Fact]
    public void SalidaIniciadaActiva_SigueHabilitada()
    {
        var salida = OptimizacionTestData.Salida();
        Assert.Empty(new ValidadorSalidaOptimizacion().Validar(salida));
    }

    [Theory]
    [InlineData(EstadoRuta.INACTIVA)]
    [InlineData(EstadoRuta.ELIMINADA)]
    public void RutaNoActiva_NoEstaHabilitada(EstadoRuta estado)
    {
        var salida = OptimizacionTestData.Salida();
        salida.Ruta.Estado = estado;
        Assert.NotEmpty(new ValidadorSalidaOptimizacion().Validar(salida));
    }

    [Fact]
    public void ViajeInactivo_NoEstaHabilitadoAunqueSuRutaEsteActiva()
    {
        var viaje = OptimizacionTestData.Viaje(OptimizacionTestData.Ruta());
        viaje.Estado = EstadoActividadViaje.INACTIVO;
        Assert.NotEmpty(new ValidadorSalidaOptimizacion().Validar(OptimizacionTestData.Salida(viaje: viaje)));
    }

    [Theory]
    [InlineData(10, 1, false)]
    [InlineData(11, 1, true)]
    [InlineData(2, 0, true)]
    public void Capacidad_DistingueLimiteExactoDeExceso(double peso, int cantidadMax, bool hayError)
    {
        var viaje = OptimizacionTestData.Viaje(OptimizacionTestData.Ruta(10, cantidadMax));
        OptimizacionTestData.Asignar(viaje, OptimizacionTestData.Envio(peso: peso));
        var salida = OptimizacionTestData.Salida(viaje: viaje);
        var compromisos = new CalculadorCompromisosSalida().Calcular(salida);
        Assert.Equal(hayError, new ValidadorSalidaOptimizacion().ValidarCapacidad(salida, compromisos).Count > 0);
    }
}
