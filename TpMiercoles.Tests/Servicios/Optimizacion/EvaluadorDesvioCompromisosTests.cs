using Moq;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class EvaluadorDesvioCompromisosTests
{
    [Theory]
    [InlineData(13000, 3, false)]
    [InlineData(15000, 5, false)]
    [InlineData(15000.1, 5.0001, true)]
    [InlineData(9000, 0, false)]
    public async Task ComparaDesvioTotalSinRedondear(double distancia, double desvio, bool supera)
    {
        var salida = OptimizacionTestData.Salida(viaje: OptimizacionTestData.Viaje(OptimizacionTestData.Ruta()));
        OptimizacionTestData.Asignar(salida.Viaje!, OptimizacionTestData.Envio());
        var proveedor = new Mock<IServicioRutas>(MockBehavior.Strict);
        proveedor.SetupSequence(p => p.CalcularRutaAsync(It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
                It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoRuta(10000, 600))
            .ReturnsAsync(new ResultadoRuta(distancia, 900));
        var resultado = await new EvaluadorDesvioCompromisos(proveedor.Object, new())
            .EvaluarAsync(salida, new CalculadorCompromisosSalida().Calcular(salida), 5);
        Assert.Equal(desvio, resultado.DesvioKm, 6);
        Assert.Equal(supera, resultado.SuperaLimite);
        Assert.Equal(10000, resultado.RutaBase.DistanciaMetros);
    }

    [Fact]
    public async Task SinCompromisos_SoloConsultaLaRutaBaseYRespetaOrdenYCancelacion()
    {
        var salida = OptimizacionTestData.Salida();
        salida.Ruta.Paradas = [new() { Orden = 1, Direccion = OptimizacionTestData.Direccion(5) }];
        using var cts = new CancellationTokenSource();
        var proveedor = new Mock<IServicioRutas>(MockBehavior.Strict);
        proveedor.Setup(p => p.CalcularRutaAsync(new Coordenada(0, 0), new Coordenada(0, 10),
                It.Is<IReadOnlyList<Coordenada>?>(p => p != null && p.Count == 1 && p[0] == new Coordenada(0, 5)), cts.Token))
            .ReturnsAsync(new ResultadoRuta(10000, 600));
        var resultado = await new EvaluadorDesvioCompromisos(proveedor.Object, new())
            .EvaluarAsync(salida, new CalculadorCompromisosSalida().Calcular(salida), 0, cts.Token);
        Assert.Equal(0, resultado.DesvioKm);
        Assert.False(resultado.SuperaLimite);
        proveedor.Verify(p => p.CalcularRutaAsync(It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
            It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task DistanciaInvalida_NoSeConvierteEnDesvioCero(double distancia)
    {
        var proveedor = new Mock<IServicioRutas>();
        proveedor.Setup(p => p.CalcularRutaAsync(It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
            It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoRuta(distancia, 600));
        var salida = OptimizacionTestData.Salida();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new EvaluadorDesvioCompromisos(proveedor.Object, new()).EvaluarAsync(
                salida, new CalculadorCompromisosSalida().Calcular(salida), 5));
    }

    [Fact]
    public async Task ErrorDelProveedor_SePropaga()
    {
        var proveedor = new Mock<IServicioRutas>();
        proveedor.Setup(p => p.CalcularRutaAsync(It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
            It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RutaNoEncontradaException("Sin conexión"));
        var salida = OptimizacionTestData.Salida();
        await Assert.ThrowsAsync<RutaNoEncontradaException>(() =>
            new EvaluadorDesvioCompromisos(proveedor.Object, new()).EvaluarAsync(
                salida, new CalculadorCompromisosSalida().Calcular(salida), 5));
    }
}
