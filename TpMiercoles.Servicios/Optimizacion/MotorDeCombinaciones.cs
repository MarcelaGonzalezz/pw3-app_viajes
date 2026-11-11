using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class MotorDeCombinaciones
    {
        public IEnumerable<IReadOnlyList<int>> GenerarCombinaciones(
            IReadOnlyList<(int EnvioId, double PesoKg, decimal Pago)> candidatos,
            double pesoDisponibleKg,
            int cantidadDisponiblePaquetes,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(candidatos);

            if (!double.IsFinite(pesoDisponibleKg) || pesoDisponibleKg < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pesoDisponibleKg));
            }

            if (cantidadDisponiblePaquetes < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cantidadDisponiblePaquetes));
            }

            var datos = candidatos.ToArray();

            if (datos.Any(candidato =>
                candidato.EnvioId <= 0
                || !double.IsFinite(candidato.PesoKg)
                || candidato.PesoKg <= 0
                || candidato.Pago <= 0))
            {
                throw new ArgumentException(
                    "Los candidatos deben tener identificador, peso "
                    + "y pago válidos.",
                    nameof(candidatos));
            }

            if (datos.Select(candidato => candidato.EnvioId)
                .Distinct()
                .Count() != datos.Length)
            {
                throw new ArgumentException(
                    "No puede haber candidatos repetidos.",
                    nameof(candidatos));
            }

            decimal limitePesoKg = (decimal)pesoDisponibleKg;

            return Explorar(
                indiceInicial: 0,
                pesoAcumuladoKg: 0m,
                seleccionados: new List<int>());

            IEnumerable<IReadOnlyList<int>> Explorar(
                int indiceInicial,
                decimal pesoAcumuladoKg,
                List<int> seleccionados)
            {
                ct.ThrowIfCancellationRequested();

                if (seleccionados.Count >= cantidadDisponiblePaquetes)
                {
                    yield break;
                }

                for (int i = indiceInicial; i < datos.Length; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var candidato = datos[i];

                    decimal nuevoPesoKg =
                        pesoAcumuladoKg + (decimal)candidato.PesoKg;

                    if (nuevoPesoKg > limitePesoKg)
                    {
                        continue;
                    }

                    var nuevaSeleccion = new List<int>(seleccionados)
                    {
                        candidato.EnvioId
                    };

                    yield return nuevaSeleccion.AsReadOnly();

                    foreach (var combinacion in Explorar(
                        i + 1,
                        nuevoPesoKg,
                        nuevaSeleccion))
                    {
                        yield return combinacion;
                    }
                }
            }
        }
    }
}
