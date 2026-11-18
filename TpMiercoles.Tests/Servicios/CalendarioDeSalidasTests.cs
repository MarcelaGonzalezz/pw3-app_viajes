using TpMiercoles.Entidades;
using TpMiercoles.Servicios;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios;

public class CalendarioDeSalidasTests
{
    // Noviembre 2026 arranca domingo, tiene cinco lunes: 2, 9, 16, 23 y 30.
    private static readonly DateTime InicioDeNoviembre = new(2026, 11, 1);
    private static readonly DateTime FinDeNoviembre = new(2026, 11, 30, 23, 59, 59);

    private static Ruta RutaDeLunes(DateTime? fechaInicio = null, DateTime? fechaFin = null)
        => RutaTestData.CrearRuta(
            fechaInicio: fechaInicio ?? new DateTime(2026, 11, 1),
            fechaFin: fechaFin,
            horaSalida: new TimeOnly(8, 0),
            dias: [DayOfWeek.Monday]);

    //EsSalidaValida 

    [Fact]
    public void EsSalidaValida_CuandoCoincidenDiaVigenciaYHora_EsVerdadero()
        => Assert.True(CalendarioDeSalidas.EsSalidaValida(
            RutaDeLunes(), new DateTime(2026, 11, 9, 8, 0, 0)));

    [Theory]
    [InlineData(2026, 11, 10, 8, 0)]   // martes: la ruta solo sale lunes
    [InlineData(2026, 10, 26, 8, 0)]   // lunes, pero anterior al inicio de vigencia
    [InlineData(2026, 11, 9, 14, 0)]   // lunes en vigencia, con otra hora
    [InlineData(2026, 11, 9, 0, 0)]    // lunes a medianoche: le falta la hora de salida
    public void EsSalidaValida_CuandoFallaCualquieraDeLosTres_EsFalso(
        int anio, int mes, int dia, int hora, int minuto)
        => Assert.False(CalendarioDeSalidas.EsSalidaValida(
            RutaDeLunes(), new DateTime(anio, mes, dia, hora, minuto, 0)));

    [Fact]
    public void EsSalidaValida_PosteriorAFechaFin_EsFalso()
        => Assert.False(CalendarioDeSalidas.EsSalidaValida(
            RutaDeLunes(fechaFin: new DateTime(2026, 11, 16)),
            new DateTime(2026, 11, 23, 8, 0, 0)));

    //--------- Todas ----------

    [Fact]
    public void Todas_DevuelveLosCincoLunesConLaHoraDeSalida()
    {
        List<DateTime> salidas = CalendarioDeSalidas.Todas(RutaDeLunes(), InicioDeNoviembre, FinDeNoviembre);

        Assert.Equal(
            new[]
            {
                new DateTime(2026, 11, 2, 8, 0, 0),
                new DateTime(2026, 11, 9, 8, 0, 0),
                new DateTime(2026, 11, 16, 8, 0, 0),
                new DateTime(2026, 11, 23, 8, 0, 0),
                new DateTime(2026, 11, 30, 8, 0, 0)
            },
            salidas);
    }

    [Fact]
    public void Todas_NoCuentaLasAnterioresAlInicioDeVigencia()
    {
        List<DateTime> salidas = CalendarioDeSalidas.Todas(
            RutaDeLunes(fechaInicio: new DateTime(2026, 11, 20)),
            InicioDeNoviembre,
            FinDeNoviembre);

        Assert.Equal(
            new[]
            {
                new DateTime(2026, 11, 23, 8, 0, 0),
                new DateTime(2026, 11, 30, 8, 0, 0)
            },
            salidas);
    }

    [Fact]
    public void Todas_NoCuentaLasPosterioresAlFinDeVigencia()
    {
        List<DateTime> salidas = CalendarioDeSalidas.Todas(
            RutaDeLunes(fechaFin: new DateTime(2026, 11, 16)),
            InicioDeNoviembre, FinDeNoviembre);

        Assert.Equal(3, salidas.Count);
        Assert.Equal(new DateTime(2026, 11, 16, 8, 0, 0), salidas[^1]);
    }

    [Fact]
    public void Todas_CuandoDesdeTieneHora_NoDevuelveLaSalidaQueYaPartio()
    {
        // Lunes 2 a las 10:00: la salida de las 08:00 de ese dia ya se fue.
        List<DateTime> salidas = CalendarioDeSalidas.Todas(
            RutaDeLunes(), new DateTime(2026, 11, 2, 10, 0, 0), FinDeNoviembre);

        Assert.Equal(4, salidas.Count);
        Assert.Equal(new DateTime(2026, 11, 9, 8, 0, 0), salidas[0]);
    }

    [Fact]
    public void Todas_RecorreTodoElMesYNoSoloLaPrimeraSemana()
    {
        // Proxima() mira siete dias; Todas() tiene que llegar hasta el final.
        List<DateTime> salidas = CalendarioDeSalidas.Todas(RutaDeLunes(), InicioDeNoviembre, FinDeNoviembre);

        Assert.Contains(new DateTime(2026, 11, 30, 8, 0, 0), salidas);
    }

    [Fact]
    public void Todas_CuandoNingunDiaCoincide_DevuelveListaVacia()
    {
        Ruta ruta = RutaTestData.CrearRuta(
            fechaInicio: new DateTime(2026, 11, 2),
            horaSalida: new TimeOnly(8, 0),
            dias: [DayOfWeek.Sunday]);

        List<DateTime> salidas = CalendarioDeSalidas.Todas(
            ruta, new DateTime(2026, 11, 2), new DateTime(2026, 11, 7, 23, 59, 59));

        Assert.Empty(salidas);
    }
}