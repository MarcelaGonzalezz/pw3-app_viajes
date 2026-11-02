using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas
{
    // Decide en que tramo del recorrido conviene pasar a retirar y a entregar un paquete.
    // El retiro va antes que la entrega y los dos quedan entre el origen y el destino del viaje.
    // Elige con distancia en linea recta (con mock gratuito); la distancia real la calcula despues IServicioRutas.
    public class PlanificadorDeRecorrido
    {
        // recorrido = origen, paradas en orden, destino.
        // Devuelve el mismo recorrido con retiro y entrega insertados donde menos lo alargan
        public static List<Coordenada> InsertarEnvio(IReadOnlyList<Coordenada> recorrido, Coordenada retiro, Coordenada entrega)
        {
            List<Coordenada?> mejor = null;
            double mejorDistanciaKm = double.MaxValue;

            for (int i = 1; i < recorrido.Count; i++)
            {
                for (int j = 1; j < recorrido.Count; j++)
                {
                    List<Coordenada> candidato = [.. recorrido]; // copia los puntos del recorrido original
                    candidato.Insert(j, entrega); // primero la entrega para no correr el indice i
                    candidato.Insert(i, retiro);

                    double distanciaKm = Geo.LongitudKm(candidato); // distancia en linea recta entre todos los puntos, incluyendo retiro y entrega
                    if (distanciaKm < mejorDistanciaKm)
                    {
                        mejorDistanciaKm = distanciaKm;
                        mejor = candidato;
                    }
                }
            }

            return mejor ?? [.. recorrido];
        }

    }
}
