using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TpMiercoles.Servicios.Excepciones;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Servicios.Rutas.OpenRouteService;
using TpMiercoles.Tests.Helpers;

namespace TpMiercoles.Tests.Servicios.Rutas {
    public class ServicioRutasConRespaldoTests {
        private static readonly Coordenada Origen = new(-34.60m, -58.38m);
        private static readonly Coordenada Destino = new(-34.65m, -58.60m);

        private static readonly ResultadoRuta RutaReal = new(12000, 900, "{\"type\":\"LineString\",\"coordinates\":[]}");
        private static readonly ResultadoRuta RutaEstimada = new(15000, 1350);

        private readonly Mock<IServicioRutas> _ors = new();
        private readonly Mock<IServicioRutas> _mock = new();
        private readonly EstadoCuotaRutas _estado = new();
        private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero));
        private readonly ServicioRutasConRespaldo _servicio;

        public ServicioRutasConRespaldoTests() {
            _mock.Setup(m => m.CalcularRutaAsync(Origen, Destino, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RutaEstimada);

            _servicio = new ServicioRutasConRespaldo(
                _ors.Object, _mock.Object, _estado, _reloj, NullLogger<ServicioRutasConRespaldo>.Instance);
        }

        #region Funcionamiento normal

        [Fact]
        public async Task CalcularRutaAsync_CuandoOrsResponde_DevuelveLaRutaRealSinUsarElMock() {
            ConfigurarOrs(RutaReal);

            ResultadoRuta resultado = await _servicio.CalcularRutaAsync(Origen, Destino);

            Assert.Same(RutaReal, resultado);
            VerificarLlamadasAlMock(Times.Never());
        }

        #endregion

        #region Cuota agotada

        [Theory]
        [InlineData(403)]
        [InlineData(429)]
        public async Task CalcularRutaAsync_CuandoOrsAgotaLaCuota_DevuelveLaRutaEstimada(int codigo) {
            ConfigurarOrsConError(codigo);

            ResultadoRuta resultado = await _servicio.CalcularRutaAsync(Origen, Destino);

            Assert.Same(RutaEstimada, resultado);
        }

        [Theory]
        [InlineData(401)]
        [InlineData(500)]
        [InlineData(502)]
        [InlineData(503)]
        public async Task CalcularRutaAsync_CuandoOrsFalla_DevuelveLaRutaEstimada(int codigo) {
            ConfigurarOrsConError(codigo);

            ResultadoRuta resultado = await _servicio.CalcularRutaAsync(Origen, Destino);

            Assert.Same(RutaEstimada, resultado);
        }

        [Fact]
        public async Task CalcularRutaAsync_SinConexionOSinRespuestaATiempo_DevuelveLaRutaEstimada() {
            // sin codigo HTTP: falla de red, tiempo de espera agotado o respuesta inesperada
            _ors.Setup(o => o.CalcularRutaAsync(Origen, Destino, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RutaNoEncontradaException("OpenRouteService no respondió a tiempo."));

            ResultadoRuta resultado = await _servicio.CalcularRutaAsync(Origen, Destino);

            Assert.Same(RutaEstimada, resultado);
        }

        [Fact]
        public async Task CalcularRutaAsync_DespuesDeUnaFalla_VuelveAConsultarOrsAlMinuto() {
            ConfigurarOrsConError(500);
            await _servicio.CalcularRutaAsync(Origen, Destino);

            ConfigurarOrs(RutaReal);
            _reloj.Fijar(_reloj.GetUtcNow().AddMinutes(2));
            ResultadoRuta resultado = await _servicio.CalcularRutaAsync(Origen, Destino);

            Assert.Same(RutaReal, resultado);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(404)]
        public async Task CalcularRutaAsync_CuandoElPedidoEsInvalidoONoHayCamino_SigueInformandoElError(int codigo) {
            ConfigurarOrsConError(codigo);

            var error = await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => _servicio.CalcularRutaAsync(Origen, Destino));

            Assert.Equal(codigo, error.CodigoHttp);
            VerificarLlamadasAlMock(Times.Never());
        }

        [Fact]
        public async Task CalcularRutaAsync_DespuesDeUn403_NoVuelveAConsultarOrsDuranteLaPausa() {
            ConfigurarOrsConError(403);
            await _servicio.CalcularRutaAsync(Origen, Destino);

            _reloj.Fijar(_reloj.GetUtcNow().AddMinutes(30));
            ResultadoRuta resultado = await _servicio.CalcularRutaAsync(Origen, Destino);

            Assert.Same(RutaEstimada, resultado);
            VerificarLlamadasAOrs(Times.Once());
        }

        [Fact]
        public async Task CalcularRutaAsync_CuandoTerminaLaPausaDeUn429_VuelveAConsultarOrs() {
            ConfigurarOrsConError(429);
            await _servicio.CalcularRutaAsync(Origen, Destino);

            ConfigurarOrs(RutaReal);
            _reloj.Fijar(_reloj.GetUtcNow().AddMinutes(2));
            ResultadoRuta resultado = await _servicio.CalcularRutaAsync(Origen, Destino);

            Assert.Same(RutaReal, resultado);
            VerificarLlamadasAOrs(Times.Exactly(2));
        }

        #endregion

        #region Código HTTP en OpenRouteServiceRutas

        [Theory]
        [InlineData(HttpStatusCode.Forbidden, 403)]
        [InlineData(HttpStatusCode.TooManyRequests, 429)]
        public async Task OpenRouteServiceRutas_CuandoRespondeConError_InformaElCodigoHttp(
            HttpStatusCode estado, int codigoEsperado) {
            using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(estado));
            using var cliente = new HttpClient(handler) { BaseAddress = new Uri("https://ors.test/") };
            var ors = new OpenRouteServiceRutas(cliente, new RutasOptions { OrsApiKey = "clave-ficticia" });

            var error = await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => ors.CalcularRutaAsync(Origen, Destino));

            Assert.Equal(codigoEsperado, error.CodigoHttp);
        }

        [Fact]
        public async Task OpenRouteServiceRutas_SiNoRespondeATiempo_LoInformaComoFallaDeOrs() {
            using var handler = new StubHttpMessageHandler(_ =>
                throw new TaskCanceledException("Timeout", new TimeoutException()));
            using var cliente = new HttpClient(handler) { BaseAddress = new Uri("https://ors.test/") };
            var ors = new OpenRouteServiceRutas(cliente, new RutasOptions { OrsApiKey = "clave-ficticia" });

            var error = await Assert.ThrowsAsync<RutaNoEncontradaException>(
                () => ors.CalcularRutaAsync(Origen, Destino));

            Assert.Null(error.CodigoHttp);
            Assert.IsType<TaskCanceledException>(error.InnerException);
        }

        #endregion

        #region Helpers

        private void ConfigurarOrs(ResultadoRuta resultado) {
            _ors.Setup(o => o.CalcularRutaAsync(Origen, Destino, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultado);
        }

        private void ConfigurarOrsConError(int codigo) {
            _ors.Setup(o => o.CalcularRutaAsync(Origen, Destino, null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RutaNoEncontradaException($"OpenRouteService respondió con el código {codigo}.") {
                    CodigoHttp = codigo
                });
        }

        private void VerificarLlamadasAOrs(Times veces) {
            _ors.Verify(o => o.CalcularRutaAsync(
                It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
                It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()), veces);
        }

        private void VerificarLlamadasAlMock(Times veces) {
            _mock.Verify(m => m.CalcularRutaAsync(
                It.IsAny<Coordenada>(), It.IsAny<Coordenada>(),
                It.IsAny<IReadOnlyList<Coordenada>?>(), It.IsAny<CancellationToken>()), veces);
        }

        #endregion
    }
}