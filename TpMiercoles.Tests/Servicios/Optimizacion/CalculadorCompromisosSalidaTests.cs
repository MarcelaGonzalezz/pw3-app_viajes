using TpMiercoles.Entidades;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class CalculadorCompromisosSalidaTests
{
    [Fact]
    public void SinViaje_TodaLaCapacidadEstaDisponible()
    {
        var resultado = new CalculadorCompromisosSalida().Calcular(OptimizacionTestData.Salida());
        Assert.Empty(resultado.Envios);
        Assert.Equal(10, resultado.PesoDisponibleKg);
        Assert.Equal(3, resultado.CantidadDisponiblePaquetes);
        Assert.Equal(0m, resultado.IngresoComprometido);
    }

    [Fact]
    public void Compromisos_ExcluyeCanceladosYUsaLimitesDelViaje()
    {
        var viaje = OptimizacionTestData.Viaje(OptimizacionTestData.Ruta(100, 10));
        viaje.PesoMax = 10;
        viaje.CapacidadMaxPaquetes = 3;
        OptimizacionTestData.Asignar(viaje, OptimizacionTestData.Envio(1, 3, 1000));
        OptimizacionTestData.Asignar(viaje, OptimizacionTestData.Envio(2, 4, 2000), EstadoViaje.PENDIENTE);
        OptimizacionTestData.Asignar(viaje, OptimizacionTestData.Envio(3, 90, 9000), EstadoViaje.CANCELADO);
        var resultado = new CalculadorCompromisosSalida().Calcular(OptimizacionTestData.Salida(viaje: viaje));
        Assert.Equal(new[] { 1, 2 }, resultado.Envios.Select(e => e.Id));
        Assert.Equal(7, resultado.PesoComprometidoKg);
        Assert.Equal(3, resultado.PesoDisponibleKg);
        Assert.Equal(1, resultado.CantidadDisponiblePaquetes);
        Assert.Equal(3000m, resultado.IngresoComprometido);
    }

    [Fact]
    public void Exceso_NoOcultaCapacidadNegativa()
    {
        var viaje = OptimizacionTestData.Viaje(OptimizacionTestData.Ruta(1, 1));
        OptimizacionTestData.Asignar(viaje, OptimizacionTestData.Envio(1, 2));
        OptimizacionTestData.Asignar(viaje, OptimizacionTestData.Envio(2, 3));
        var resultado = new CalculadorCompromisosSalida().Calcular(OptimizacionTestData.Salida(viaje: viaje));
        Assert.Equal(-4, resultado.PesoDisponibleKg);
        Assert.Equal(-1, resultado.CantidadDisponiblePaquetes);
    }

    [Fact]
    public void EnvioNoCargado_NoLoTrataComoPesoCero()
    {
        var viaje = OptimizacionTestData.Viaje(OptimizacionTestData.Ruta());
        viaje.Asignaciones.Add(new Asignacion { Estado = EstadoViaje.CONFIRMADO, Envio = null! });
        Assert.Throws<InvalidOperationException>(() => new CalculadorCompromisosSalida()
            .Calcular(OptimizacionTestData.Salida(viaje: viaje)));
    }

    [Fact]
    public void Entregado_NoLiberaCapacidadEnElModeloDePesoTotal()
    {
        var viaje = OptimizacionTestData.Viaje(OptimizacionTestData.Ruta());
        var envio = OptimizacionTestData.Envio(peso: 4);
        envio.Estado = EstadoEnvio.Entregado;
        OptimizacionTestData.Asignar(viaje, envio).FechaEntrega = OptimizacionTestData.Fecha.AddHours(1);
        var resultado = new CalculadorCompromisosSalida().Calcular(OptimizacionTestData.Salida(viaje: viaje));
        Assert.Equal(6, resultado.PesoDisponibleKg);
        Assert.Equal(2, resultado.CantidadDisponiblePaquetes);
    }
}
