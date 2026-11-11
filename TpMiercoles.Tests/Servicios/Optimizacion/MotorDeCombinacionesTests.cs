using TpMiercoles.Servicios.Optimizacion;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class MotorDeCombinacionesTests
{
    private readonly MotorDeCombinaciones _motor = new();

    [Fact]
    public void Peso_DescartaRamaPesadaPeroSigueBuscandoCandidatosLivianos()
    {
        var resultado = _motor.GenerarCombinaciones(
            [(1, 6d, 4000m), (2, 8d, 5000m), (3, 3d, 2000m)], 10, 2);
        Assert.Equal(new[] { "1", "1,3", "2", "3" }, Claves(resultado));
    }

    [Fact]
    public void Cantidad_DescartaTriplesAunqueElPesoAlcance()
    {
        var resultado = _motor.GenerarCombinaciones(
            [(1, 1d, 10m), (2, 1d, 20m), (3, 1d, 30m)], 100, 2);
        Assert.Equal(new[] { "1", "1,2", "1,3", "2", "2,3", "3" }, Claves(resultado));
    }

    [Fact]
    public void LimitesExactos_PermiteCombinacionEnElLimite()
    {
        Assert.Equal(new[] { "1", "1,2", "2" }, Claves(
            _motor.GenerarCombinaciones([(1, 4d, 1m), (2, 6d, 1m)], 10, 2)));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(10, 0)]
    public void SinCapacidad_NoGeneraCombinaciones(double peso, int cantidad)
        => Assert.Empty(_motor.GenerarCombinaciones([(1, 1d, 1m)], peso, cantidad));

    [Fact]
    public void SinCandidatos_NoGeneraCombinacionVacia()
        => Assert.Empty(_motor.GenerarCombinaciones([], 10, 3));

    [Fact]
    public void EnumeracionRepetida_NoContaminaResultadosPrevios()
    {
        var resultado = _motor.GenerarCombinaciones([(1, 1d, 1m), (2, 1d, 1m)], 10, 2);
        var primera = resultado.ToList();
        Assert.Equal(new[] { "1", "1,2", "2" }, Claves(resultado));
        Assert.Equal(new[] { 1 }, primera[0]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void PesoDisponibleInvalido_SeRechaza(double peso)
        => Assert.Throws<ArgumentOutOfRangeException>(() => _motor.GenerarCombinaciones([], peso, 1));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void PesoCandidatoInvalido_SeRechaza(double peso)
        => Assert.Throws<ArgumentException>(() => _motor.GenerarCombinaciones([(1, peso, 1m)], 10, 2));

    [Fact]
    public void IdentificadoresRepetidos_SeRechazan()
        => Assert.Throws<ArgumentException>(() => _motor.GenerarCombinaciones([(1, 1d, 1m), (1, 2d, 2m)], 10, 2));

    [Fact]
    public void CancelacionDuranteEnumeracion_InterrumpeLaBusqueda()
    {
        using var cancelacion = new CancellationTokenSource();
        using var enumerador = _motor.GenerarCombinaciones(
            [(1, 1d, 1m), (2, 1d, 1m)], 10, 2, cancelacion.Token).GetEnumerator();
        Assert.True(enumerador.MoveNext());
        cancelacion.Cancel();
        Assert.Throws<OperationCanceledException>(() => enumerador.MoveNext());
    }

    [Fact]
    public void ConjuntosPequenos_CoincidenConOraculoExhaustivoIndependiente()
    {
        // El oráculo enumera máscaras binarias, no usa la recursión del motor.
        var candidatos = new[] { (1, 2d, 10m), (2, 5d, 30m), (3, 3d, 20m), (4, 7d, 40m) };
        for (int pesoMax = 0; pesoMax <= 17; pesoMax++)
        for (int cantidadMax = 0; cantidadMax <= 4; cantidadMax++)
        {
            var esperadas = new List<string>();
            for (int mascara = 1; mascara < 16; mascara++)
            {
                var subconjunto = candidatos.Where((_, i) => (mascara & (1 << i)) != 0).ToArray();
                if (subconjunto.Length <= cantidadMax && subconjunto.Sum(c => c.Item2) <= pesoMax)
                    esperadas.Add(string.Join(",", subconjunto.Select(c => c.Item1)));
            }
            Assert.Equal(esperadas.OrderBy(c => c), Claves(
                _motor.GenerarCombinaciones(candidatos, pesoMax, cantidadMax)));
        }
    }

    private static string[] Claves(IEnumerable<IReadOnlyList<int>> combinaciones)
        => combinaciones.Select(c => string.Join(",", c.OrderBy(id => id))).OrderBy(c => c).ToArray();

    [Fact]
    public void PesosFraccionarios_AdmiteCienYDosCientosGramosConLimiteDeTrescientos()
    {
        // Regresión: 0.1 + 0.2 en double no debe descartar un paquete físicamente válido.
        var resultado = _motor.GenerarCombinaciones([(1, 0.1d, 1m), (2, 0.2d, 1m)], 0.3d, 2);
        Assert.Equal(new[] { "1", "1,2", "2" }, Claves(resultado));
    }
}
