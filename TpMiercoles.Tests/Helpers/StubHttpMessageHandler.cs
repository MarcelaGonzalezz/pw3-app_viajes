namespace TpMiercoles.Tests.Helpers
{
    // Handler falso para los tests que ejercitan HttpClient sin salir a la red.
    // Devuelve la respuesta que arma cada test y registra qué se envió, así se
    // puede verificar la URL y la cookie reenviada sin levantar la API real.
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public int Llamadas { get; private set; }
        public string? UltimaUrl { get; private set; }
        public string? UltimaCookie { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Llamadas++;
            UltimaUrl = request.RequestUri?.ToString();
            UltimaCookie = request.Headers.TryGetValues("Cookie", out IEnumerable<string>? valores)
                ? string.Join("; ", valores)
                : null;

            return Task.FromResult(_responder(request));
        }
    }
}
