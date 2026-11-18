using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Servicios.Rutas;
using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class ConstructorRecorridoOptimizacion
    {
        public IReadOnlyList<Coordenada> ConstruirBase(SalidaOptimizacion salida) {
            ArgumentNullException.ThrowIfNull(salida);

            var recorrido = new List<Coordenada>
            {
                new Coordenada(
                    salida.Origen.Latitud,
                    salida.Origen.Longitud)
            };

            foreach (var parada in salida.Paradas)
            {
                recorrido.Add(new Coordenada(
                    parada.Direccion.Latitud,
                    parada.Direccion.Longitud));
            }

            recorrido.Add(new Coordenada(
                salida.Destino.Latitud,
                salida.Destino.Longitud));

            return recorrido.AsReadOnly();
        }

        public IReadOnlyList<Coordenada> ConstruirConEnvios(
    IReadOnlyList<Coordenada> recorridoBase,
    IReadOnlyList<Envio> envios,
    CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(recorridoBase);
            ArgumentNullException.ThrowIfNull(envios);

            if (recorridoBase.Count < 2)
            {
                throw new ArgumentException(
                    "El recorrido debe contener al menos origen y destino.",
                    nameof(recorridoBase));
            }

            var recorrido = new List<Coordenada>(recorridoBase);

            foreach (var envio in envios.OrderBy(envio => envio.Id))
            {
                ct.ThrowIfCancellationRequested();

                if (envio.DireccionRetiro is null
                    || envio.DireccionEntrega is null)
                {
                    throw new InvalidOperationException(
                        $"El envío {envio.Id} debe tener cargadas "
                        + "sus direcciones de retiro y entrega.");
                }

                var retiro = new Coordenada(
                    envio.DireccionRetiro.Latitud,
                    envio.DireccionRetiro.Longitud);

                var entrega = new Coordenada(
                    envio.DireccionEntrega.Latitud,
                    envio.DireccionEntrega.Longitud);

                recorrido = PlanificadorDeRecorrido.InsertarEnvio(
                    recorrido,
                    retiro,
                    entrega);
            }

            return recorrido.AsReadOnly();
        }
    }
}
