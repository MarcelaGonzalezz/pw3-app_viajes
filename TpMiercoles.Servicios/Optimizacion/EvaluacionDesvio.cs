
using TpMiercoles.Servicios.Rutas;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class EvaluacionDesvio
    {
        public required ResultadoRuta RutaBase { get; init; }

        public required ResultadoRuta RutaConEnvios { get; init; }

        public required double DesvioMaximoKm { get; init; }

        public double DesvioKm => Math.Max(
            0,
            RutaConEnvios.DistanciaKm - RutaBase.DistanciaKm);

        public bool SuperaLimite => DesvioKm > DesvioMaximoKm;
    }
}
