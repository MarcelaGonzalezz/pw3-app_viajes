using Moq;
using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class BuscadorCandidatosOptimizacionTests
{
    [Fact]
    public async Task BuscaConMomentoEvaluacionYPropietario_YFiltraPesoYPago()
    {
        var repositorio = new Mock<IEnvioRepositorio>(MockBehavior.Strict);
        var ruta = OptimizacionTestData.Ruta();
        var salida = new SalidaOptimizacion
        {
            Ruta = ruta, FechaSalidaProgramada = OptimizacionTestData.Fecha,
            MomentoEvaluacion = OptimizacionTestData.Fecha.AddHours(2)
        };
        repositorio.Setup(r => r.ListarDisponiblesAsync(salida.MomentoEvaluacion, 7)).ReturnsAsync(
        [
            OptimizacionTestData.Envio(1, 6), OptimizacionTestData.Envio(2, 8),
            OptimizacionTestData.Envio(3, 12), OptimizacionTestData.Envio(4, 0),
            OptimizacionTestData.Envio(5, -1), OptimizacionTestData.Envio(6, double.NaN),
            OptimizacionTestData.Envio(7, double.PositiveInfinity),
            OptimizacionTestData.Envio(8, 1, 0), OptimizacionTestData.Envio(9, 10),
            OptimizacionTestData.Envio(10, 1, -100)
        ]);
        var compromisos = new CalculadorCompromisosSalida().Calcular(salida);
        var resultado = await new BuscadorCandidatosOptimizacion(repositorio.Object).BuscarAsync(salida, compromisos);
        // No consume capacidad al agregar cada candidato: 6, 8 y 10 caben individualmente.
        Assert.Equal(new[] { 1, 2, 9 }, resultado.Select(e => e.Id));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(10, 0)]
    public async Task SinCapacidad_NoConsultaLaBase(double peso, int cantidad)
    {
        var repositorio = new Mock<IEnvioRepositorio>(MockBehavior.Strict);
        var compromisos = new CompromisosSalida
        {
            Envios = [], PesoComprometidoKg = 0, IngresoComprometido = 0,
            PesoDisponibleKg = peso, CantidadDisponiblePaquetes = cantidad
        };
        var resultado = await new BuscadorCandidatosOptimizacion(repositorio.Object)
            .BuscarAsync(OptimizacionTestData.Salida(), compromisos);
        Assert.Empty(resultado);
        repositorio.VerifyNoOtherCalls();
    }
}
