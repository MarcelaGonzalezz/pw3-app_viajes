using TpMiercoles.Entidades;
using TpMiercoles.Servicios.Optimizacion;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class RecorridosOptimizacionTests
{
    [Fact]
    public void Base_OrdenaParadasSinModificarLaRuta()
    {
        var ruta = OptimizacionTestData.Ruta();
        ruta.Paradas =
        [
            new Parada { Id = 2, Orden = 2, Direccion = OptimizacionTestData.Direccion(6) },
            new Parada { Id = 1, Orden = 1, Direccion = OptimizacionTestData.Direccion(3) }
        ];
        var resultado = new ConstructorRecorridoOptimizacion().ConstruirBase(OptimizacionTestData.Salida(ruta));
        Assert.Equal(new decimal[] { 0, 3, 6, 10 }, resultado.Select(c => c.Longitud));
        Assert.Equal(new[] { 2, 1 }, ruta.Paradas.Select(p => p.Id));
    }

    [Fact]
    public void Base_UsaSnapshotDelViajeEnLugarDePlantillaEditada()
    {
        var ruta = OptimizacionTestData.Ruta();
        var viaje = OptimizacionTestData.Viaje(ruta);
        viaje.DireccionOrigen = OptimizacionTestData.Direccion(1);
        viaje.DireccionDestino = OptimizacionTestData.Direccion(9);
        viaje.Paradas = [new Parada { Orden = 1, Direccion = OptimizacionTestData.Direccion(4) }];
        var resultado = new ConstructorRecorridoOptimizacion().ConstruirBase(OptimizacionTestData.Salida(viaje: viaje));
        Assert.Equal(new decimal[] { 1, 4, 9 }, resultado.Select(c => c.Longitud));
    }

    [Fact]
    public void Insercion_NoEntregaAntesDeRetirarAunqueSeaMasCorto()
    {
        // La entrega está cerca del origen y el retiro cerca del destino.
        // Sin precedencia, el planificador elegiría incorrectamente entrega -> retiro.
        Coordenada[] original = [new(0, 0), new(0, 5), new(0, 10)];
        var retiro = new Coordenada(0, 8);
        var entrega = new Coordenada(0, 2);
        var resultado = PlanificadorDeRecorrido.InsertarEnvio(original, retiro, entrega);
        Assert.True(resultado.IndexOf(retiro) < resultado.IndexOf(entrega));
        Assert.Equal(original[0], resultado[0]);
        Assert.Equal(original[^1], resultado[^1]);
        Assert.Equal(new decimal[] { 0, 5, 10 }, original.Select(c => c.Longitud));
        Assert.Equal(5, resultado.Count);
    }

    [Fact]
    public void Insercion_SinParadasConservaAmbosExtremos()
    {
        var resultado = PlanificadorDeRecorrido.InsertarEnvio(
            [new(0, 0), new(0, 10)], new(0, 2), new(0, 8));
        Assert.Equal(new decimal[] { 0, 2, 8, 10 }, resultado.Select(c => c.Longitud));
    }

    [Fact]
    public void VariosEnvios_ConservaParadasYPrecedenciaDeCadaPaquete()
    {
        Coordenada[] original = [new(0, 0), new(0, 3), new(0, 6), new(0, 10)];
        var a = OptimizacionTestData.Envio(1);
        var b = OptimizacionTestData.Envio(2);
        a.DireccionRetiro = OptimizacionTestData.Direccion(8);
        a.DireccionEntrega = OptimizacionTestData.Direccion(2);
        b.DireccionRetiro = OptimizacionTestData.Direccion(7);
        b.DireccionEntrega = OptimizacionTestData.Direccion(1);
        var constructor = new ConstructorRecorridoOptimizacion();
        var resultado = constructor.ConstruirConEnvios(original, [b, a]).ToList();
        Assert.Equal(8, resultado.Count);
        Assert.Equal(original, resultado.Where(c => original.Contains(c)));
        Assert.True(resultado.IndexOf(new(0, 8)) < resultado.IndexOf(new(0, 2)));
        Assert.True(resultado.IndexOf(new(0, 7)) < resultado.IndexOf(new(0, 1)));
        Assert.Equal(resultado, constructor.ConstruirConEnvios(original, [a, b]));
        Assert.Equal(4, original.Length);
    }

    [Fact]
    public void SinEnvios_DevuelveCopiaDeLaBase()
    {
        var original = new List<Coordenada> { new(0, 0), new(0, 10) };
        var resultado = new ConstructorRecorridoOptimizacion().ConstruirConEnvios(original, []);
        original.Clear();
        Assert.Equal(new[] { new Coordenada(0, 0), new Coordenada(0, 10) }, resultado);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DireccionNoCargada_NoOmiteElPaquete(bool faltaRetiro)
    {
        var envio = OptimizacionTestData.Envio();
        if (faltaRetiro) envio.DireccionRetiro = null!;
        else envio.DireccionEntrega = null!;
        Assert.Throws<InvalidOperationException>(() => new ConstructorRecorridoOptimizacion()
            .ConstruirConEnvios([new(0, 0), new(0, 10)], [envio]));
    }

    [Fact]
    public void RecorridoSinDestino_SeRechaza()
        => Assert.Throws<ArgumentException>(() => PlanificadorDeRecorrido.InsertarEnvio(
            [new(0, 0)], new(0, 1), new(0, 2)));

    [Fact]
    public void Cancelacion_NoContinuaInsertandoEnvios()
        => Assert.Throws<OperationCanceledException>(() => new ConstructorRecorridoOptimizacion()
            .ConstruirConEnvios([new(0, 0), new(0, 10)], [OptimizacionTestData.Envio()], new CancellationToken(true)));
}
