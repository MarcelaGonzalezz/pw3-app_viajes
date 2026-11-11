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
        public static List<Coordenada> InsertarEnvio(
    IReadOnlyList<Coordenada> recorrido,
    Coordenada retiro,
    Coordenada entrega)
        {
            ArgumentNullException.ThrowIfNull(recorrido);
            ArgumentNullException.ThrowIfNull(retiro);
            ArgumentNullException.ThrowIfNull(entrega);

            if (recorrido.Count < 2)
            {
                throw new ArgumentException(
                    "El recorrido debe contener al menos origen y destino.",
                    nameof(recorrido));
            }

            List<Coordenada>? mejorRecorrido = null;
            double menorDistanciaKm = double.PositiveInfinity;

            for (int posicionRetiro = 1; posicionRetiro < recorrido.Count; posicionRetiro++)
            {
                for (int posicionEntrega = posicionRetiro; posicionEntrega < recorrido.Count; posicionEntrega++)
                {
                    var candidato = new List<Coordenada>(recorrido);

                    candidato.Insert(posicionEntrega, entrega);
                    candidato.Insert(posicionRetiro, retiro);

                    double distanciaKm = Geo.LongitudKm(candidato);

                    if (distanciaKm < menorDistanciaKm)
                    {
                        menorDistanciaKm = distanciaKm;
                        mejorRecorrido = candidato;
                    }
                }
            }

            return mejorRecorrido
                ?? throw new InvalidOperationException(
                    "No se pudo construir un recorrido válido para el envío.");
        }

    }
}
