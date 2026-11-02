using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas
{
    public static class Geo
    {
        private const double RadioTierraKm = 6371;

        /// <summary>
        /// Calcula la distancia en linea recta (formula de Haversine). Utilizada en el MockServicioRutas y oportunidades para elegir
        /// donde insertar un envio sin gastar llamadas a ORS.
        /// </summary>
        /// <param name="a">Coordenada geográfica de origen.</param>
        /// <param name="b">Coordenada geográfica de destino.</param>
        /// <returns>Distancia aproximada, en kilómetros, entre las coordenadas indicadas.</returns>
        public static double DistanciaKm(Coordenada a, Coordenada b)
        {
            double lat1 = Radianes(a.Latitud);
            double lat2 = Radianes(b.Latitud);
            double distanciaLat = lat2  - lat1;
            double distanciaLon = Radianes(b.Longitud) - Radianes(a.Longitud); 

            double h = Math.Sin(distanciaLat / 2) * Math.Sin(distanciaLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(distanciaLon / 2) * Math.Sin(distanciaLon / 2);
            
            return 2 * RadioTierraKm * Math.Asin(Math.Sqrt(h));
        }

        /// <summary>
        /// Calcula el largo total de un recorrido que pasa por los puntos en el orden dado.
        /// </summary>
        /// <param name="puntos">Puntos que forman la ruta.</param>
        /// <returns>La longitud total de la ruta, en kilómetros.</returns>
        public static double LongitudKm(IReadOnlyList<Coordenada> puntos)
        {
            double total = 0;
            for (int i = 1; i < puntos.Count; i++)
            {
                total += DistanciaKm(puntos[i - 1], puntos[i]);
            }
            return total;
        }

        /// <summary>
        /// Convierte un ángulo expresado en grados a radianes.
        /// </summary>
        /// <param name="grados">Ángulo expresado en grados.</param>
        /// <returns>El ángulo convertido a radianes.</returns>
        private static double Radianes(decimal grados)
        {
            return (double)grados * Math.PI / 180;
        }
    }
}
