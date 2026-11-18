using System.Net;
using System.Text;
using System.Text.Json;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Servicios.Rutas.OpenRouteService;

namespace TpMiercoles.Tests.Servicios.Optimizacion;

public class OpenRouteServiceOptimizacionTests
{
    [Fact]
    public async Task Pedido_ConservaOrdenYEnviaLongitudAntesDeLatitud_RespuestaConservaUnidades()
    {
        using var handler = new Handler(async (pedido, ct) =>
        {
            Assert.Equal(HttpMethod.Post, pedido.Method);
            Assert.Equal("/v2/directions/driving-car/geojson", pedido.RequestUri!.AbsolutePath);
            Assert.Equal("clave-ficticia", Assert.Single(pedido.Headers.GetValues("Authorization")));
            using var cuerpo = JsonDocument.Parse(await pedido.Content!.ReadAsStringAsync(ct));
            var puntos = cuerpo.RootElement.GetProperty("coordinates").EnumerateArray()
                .Select(p => p.EnumerateArray().Select(n => n.GetDouble()).ToArray()).ToArray();
            Assert.Equal(3, puntos.Length);
            Assert.Equal(new double[] { -58, -34 }, puntos[0]);
            Assert.Equal(new double[] { -57, -33 }, puntos[1]);
            Assert.Equal(new double[] { -56, -32 }, puntos[2]);
            Assert.True(cuerpo.RootElement.GetProperty("geometry").GetBoolean());
            Assert.False(cuerpo.RootElement.GetProperty("instructions").GetBoolean());
            return Respuesta("""
                {"features":[{"properties":{"summary":{"distance":12500,"duration":900}},
                "geometry":{"type":"LineString","coordinates":[[-58,-34],[-56,-32]]}}]}
                """);
        });
        using var cliente = Cliente(handler);
        var servicio = new OpenRouteServiceRutas(cliente, new RutasOptions { OrsApiKey = "clave-ficticia" });
        var resultado = await servicio.CalcularRutaAsync(new(-34, -58), new(-32, -56), [new(-33, -57)]);
        Assert.Equal(12500, resultado.DistanciaMetros);
        Assert.Equal(12.5, resultado.DistanciaKm);
        Assert.Equal(15, resultado.DuracionMinutos);
        using var geometria = JsonDocument.Parse(resultado.GeometriaJson!);
        Assert.Equal("LineString", geometria.RootElement.GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(401)]
    [InlineData(429)]
    [InlineData(500)]
    public async Task ErrorHttp_NoDevuelveDistanciaCero(int estado)
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)estado)));
        using var cliente = Cliente(handler);
        await Assert.ThrowsAsync<RutaNoEncontradaException>(() =>
            new OpenRouteServiceRutas(cliente, new()).CalcularRutaAsync(new(0, 0), new(0, 1)));
    }

    [Fact]
    public async Task SinFeatures_SeInformaComoRutaNoEncontrada()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Respuesta("{\"features\":[]}")));
        using var cliente = Cliente(handler);
        await Assert.ThrowsAsync<RutaNoEncontradaException>(() =>
            new OpenRouteServiceRutas(cliente, new()).CalcularRutaAsync(new(0, 0), new(0, 1)));
    }

    [Fact]
    public async Task FallaDeRed_ConservaLaCausa()
    {
        var causa = new HttpRequestException("Falla de prueba");
        using var handler = new Handler((_, _) => throw causa);
        using var cliente = Cliente(handler);
        var error = await Assert.ThrowsAsync<RutaNoEncontradaException>(() =>
            new OpenRouteServiceRutas(cliente, new()).CalcularRutaAsync(new(0, 0), new(0, 1)));
        Assert.Same(causa, error.InnerException);
    }

    [Fact]
    public async Task Cancelacion_NoSeTraduceComoRutaImposible()
    {
        using var handler = new Handler((_, ct) => Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true)));
        using var cliente = Cliente(handler);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new OpenRouteServiceRutas(cliente, new()).CalcularRutaAsync(new(0, 0), new(0, 1)));
    }

    private static HttpClient Cliente(HttpMessageHandler handler)
        => new(handler) { BaseAddress = new Uri("https://rutas.invalid/") };

    private static HttpResponseMessage Respuesta(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responder(request, cancellationToken);
    }
}
