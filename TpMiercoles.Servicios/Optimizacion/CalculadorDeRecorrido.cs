using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Servicios.Rutas;

namespace TpMiercoles.Servicios.Optimizacion
{
    public interface ICalculadorDeRecorrido
    {
        Task<ResultadoRuta> CalcularAsync(
            IReadOnlyList<Coordenada> recorrido,
            CancellationToken ct = default);
    }

    public sealed class CalculadorDeRecorrido : ICalculadorDeRecorrido
    {
        private readonly IServicioRutas _servicioRutas;

        public CalculadorDeRecorrido(IServicioRutas servicioRutas)
        {
            _servicioRutas = servicioRutas;
        }

        public async Task<ResultadoRuta> CalcularAsync(
            IReadOnlyList<Coordenada> recorrido,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(recorrido);

            if (recorrido.Count < 2)
            {
                throw new ArgumentException(
                    "El recorrido debe contener al menos origen y destino.",
                    nameof(recorrido));
            }

            Coordenada origen = recorrido[0];
            Coordenada destino = recorrido[^1];

            List<Coordenada> paradas = recorrido
                .Skip(1)
                .Take(recorrido.Count - 2)
                .ToList();

            return await _servicioRutas.CalcularRutaAsync(
                origen,
                destino,
                paradas,
                ct);
        }
    }
}
