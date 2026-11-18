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
            var cuerpo = new { coordinates = puntos, instructions = false,
                geometry = true
            };
            //Apuntamos al endpoint que soporta geojson nativo para leer las features
            using var pedido = new HttpRequestMessage(HttpMethod.Post, "v2/directions/driving-car/geojson") {
                Content = JsonContent.Create(cuerpo)
            };

            pedido.Headers.TryAddWithoutValidation("Authorization", _opciones.OrsApiKey);

            try {
                using var respuesta = await _http.SendAsync(pedido, ct);

                if (!respuesta.IsSuccessStatusCode)
                    throw new RutaNoEncontradaException(
                        $"OpenRouteService respondió con el código {(int)respuesta.StatusCode}.") {
                        CodigoHttp = (int)respuesta.StatusCode
                    };

                await using var stream = await respuesta.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                // 3. Al usar /geojson, la respuesta viene en un arreglo "features"
                var features = doc.RootElement.GetProperty("features");
                if (features.GetArrayLength() == 0)
                    // es lo mismo que un 404: no hay camino posible entre esos puntos
                    throw new RutaNoEncontradaException("OpenRouteService no devolvió ninguna ruta.") { CodigoHttp = 404 };

                var primeraFeature = features[0];
                var resumen = primeraFeature.GetProperty("properties").GetProperty("summary");
                // Capturamos la geometría como texto JSON (GeoJSON LineString)
                string geometriaJson = primeraFeature.GetProperty("geometry").GetRawText();

                return new ResultadoRuta(
                    resumen.GetProperty("distance").GetDouble(),
                    resumen.GetProperty("duration").GetDouble(), geometriaJson); 
            } catch (HttpRequestException ex) {
                throw new RutaNoEncontradaException("No se pudo conectar con OpenRouteService.", ex);
            } catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
                // se agoto la espera del HttpClient (no la cancelo el usuario): se informa como falla de ORS
                throw new RutaNoEncontradaException("OpenRouteService no respondió a tiempo.", ex);
            } catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException) {
                throw new RutaNoEncontradaException("OpenRouteService devolvió una respuesta inesperada.", ex);
            }
        }
        private static double[] Par(Coordenada c) => [(double)c.Longitud, (double)c.Latitud];


    }
}
