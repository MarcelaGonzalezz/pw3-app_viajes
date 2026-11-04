using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas {
    public class MockServicioRutas : IServicioRutas {

        private readonly RutasOptions _opciones;

        public MockServicioRutas(RutasOptions opciones)
        {
            _opciones = opciones;
        }

        /// <summary>
        /// Calcula una ruta entre un origen y un destino, pasando opcionalmente por varias paradas.
        /// </summary>
        /// <param name="origen">Coordenada inicial de la ruta.</param>
        /// <param name="destino">Coordenada final de la ruta.</param>
        /// <param name="paradas">Paradas intermedias de la ruta, o una lista vacía si no se especifican.</param>
        /// <param name="ct">Token para cancelar la operación.</param>
        /// <returns>Tarea completada con el resultado de la ruta, incluida la distancia en metros y la duración en segundos.</returns>
        public Task<ResultadoRuta> CalcularRutaAsync(Coordenada origen, Coordenada destino, IReadOnlyList<Coordenada>? paradas = null, CancellationToken ct = default) {
            List<Coordenada> puntos = [origen, .. paradas ?? [], destino];

            double km = Geo.LongitudKm(puntos) * _opciones.MockFactorCalle;
            double horas = km / _opciones.MockVelocidadKmh;

            return Task.FromResult(new ResultadoRuta(km * 1000, horas * 3600));
        }
    }
}
