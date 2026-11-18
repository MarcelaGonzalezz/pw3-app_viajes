using TpMiercoles.Entidades;
using TpMiercoles.Repositorios;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Repositorios;

public class EnviosDisponiblesOptimizacionTests
{
    [Fact]
    public async Task Disponibles_ExcluyePropiosCanceladosAsignadosYFueraDeFecha()
    {
        var valido = OptimizacionTestData.Envio(1);
        var propio = OptimizacionTestData.Envio(2);
        propio.UsuarioId = 7;
        var cancelado = OptimizacionTestData.Envio(3);
        cancelado.Estado = EstadoEnvio.Cancelado;
        var asignado = OptimizacionTestData.Envio(4);
        asignado.Estado = EstadoEnvio.ConductorAsignado;
        var conAsignacion = OptimizacionTestData.Envio(5);
        conAsignacion.Asignaciones.Add(new Asignacion { Estado = EstadoViaje.PENDIENTE });
        var futuro = OptimizacionTestData.Envio(6);
        futuro.FechaDesde = OptimizacionTestData.Fecha.AddDays(2);
        var vencido = OptimizacionTestData.Envio(7);
        vencido.FechaHasta = OptimizacionTestData.Fecha.AddDays(-1);
        var liberado = OptimizacionTestData.Envio(8);
        liberado.Asignaciones.Add(new Asignacion { Estado = EstadoViaje.CANCELADO });

        var db = new DbContextMockFactory(enviosIniciales:
            [liberado, propio, cancelado, asignado, conAsignacion, futuro, vencido, valido]);
        var resultado = await new EnvioRepositorio(db.Contexto.Object)
            .ListarDisponiblesAsync(OptimizacionTestData.Fecha, 7);

        Assert.Equal(new[] { 1, 8 }, resultado.Select(e => e.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    public async Task Disponibles_LimitesDeFechaSonInclusivosYPorDia(int hora)
    {
        var envio = OptimizacionTestData.Envio();
        envio.FechaDesde = OptimizacionTestData.Fecha.Date;
        envio.FechaHasta = OptimizacionTestData.Fecha.Date;
        var db = new DbContextMockFactory(enviosIniciales: [envio]);
        var resultado = await new EnvioRepositorio(db.Contexto.Object)
            .ListarDisponiblesAsync(OptimizacionTestData.Fecha.Date.AddHours(hora), 7);
        Assert.Equal(1, Assert.Single(resultado).Id);
    }
}
