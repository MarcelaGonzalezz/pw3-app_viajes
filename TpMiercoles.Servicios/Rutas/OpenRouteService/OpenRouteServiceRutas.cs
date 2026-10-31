using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TpMiercoles.Servicios.Excepciones;


namespace TpMiercoles.Servicios.Rutas.OpenRouteService {
    public class OpenRouteServiceRutas : IServicioRutas {
        private readonly HttpClient _http;//el que hace las llamadas por red
        private readonly RutasOptions _opciones;//para leer la KEY

        public OpenRouteServiceRutas(HttpClient http, RutasOptions opciones) {
            _http = http;
            _opciones = opciones;
        }

        public async Task<ResultadoRuta> CalcularRutaAsync(Coordenada origen, Coordenada destino, IReadOnlyList<Coordenada>? paradas = null, CancellationToken ct = default) {
            // ORS espera [longitud, latitud]: al revés de como guardamos Coordenada.

            var puntos = new List<double[]> { Par(origen) };//lista de puntos del pedido
            //par:metodo auxiliar de una sola linea, convierte una coordenada en un par de numeros( lo que espera ORS)
            puntos.AddRange((paradas ?? []).Select(Par));
            puntos.Add(Par(destino));
            //armado de puntos

            //cuerpo del pedido con instrucciones en falso para que ORS no mande los pasos ni el trazado
            var cuerpo = new { coordinates = puntos, instructions = false, geometry = false };

            using var pedido = new HttpRequestMessage(HttpMethod.Post, "v2/directions/driving-car") {
                Content = JsonContent.Create(cuerpo)
            };

            pedido.Headers.TryAddWithoutValidation("Authorization", _opciones.OrsApiKey);

            try {
                using var respuesta = await _http.SendAsync(pedido, ct);

                if (!respuesta.IsSuccessStatusCode)
                    throw new RutaNoEncontradaException(
                        $"OpenRouteService respondió con el código {(int)respuesta.StatusCode}.");

                await using var stream = await respuesta.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                var rutas = doc.RootElement.GetProperty("routes");
                if (rutas.GetArrayLength() == 0)
                    throw new RutaNoEncontradaException("OpenRouteService no devolvió ninguna ruta.");

                var resumen = rutas[0].GetProperty("summary");
                return new ResultadoRuta(
                    resumen.GetProperty("distance").GetDouble(),
                    resumen.GetProperty("duration").GetDouble());
            } catch (HttpRequestException ex) {
                throw new RutaNoEncontradaException("No se pudo conectar con OpenRouteService.", ex);
            }
        }
        private static double[] Par(Coordenada c) => [(double)c.Longitud, (double)c.Latitud];


    }
}
