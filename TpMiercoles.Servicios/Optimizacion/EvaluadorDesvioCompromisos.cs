using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Servicios.Rutas;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class EvaluadorDesvioCompromisos
    {
        private readonly IServicioRutas _servicioRutas;
        private readonly ConstructorRecorridoOptimizacion _constructor;

        public EvaluadorDesvioCompromisos(
            IServicioRutas servicioRutas,
            ConstructorRecorridoOptimizacion constructor)
        {
            _servicioRutas = servicioRutas;
            _constructor = constructor;
        }

        public Task<EvaluacionDesvio> EvaluarAsync(
            SalidaOptimizacion salida,
            CompromisosSalida compromisos,
            double desvioMaximoKm,
            CancellationToken ct = default)
            => EvaluarAsync(salida, compromisos, desvioMaximoKm, _servicioRutas, ct);

        // Igual, pero con el servicio de rutas que se indique (el estimado o el real).
        public async Task<EvaluacionDesvio> EvaluarAsync(
            SalidaOptimizacion salida,
            CompromisosSalida compromisos,
            double desvioMaximoKm,
            IServicioRutas servicioRutas,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(salida);
            ArgumentNullException.ThrowIfNull(compromisos);

            if (!double.IsFinite(desvioMaximoKm) || desvioMaximoKm < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(desvioMaximoKm),
                    "El desvío máximo debe ser finito y no negativo.");
            }

            var recorridoBase = _constructor.ConstruirBase(salida);

            ResultadoRuta rutaBase =
                await servicioRutas.CalcularRutaAsync(
                    recorridoBase[0],
                    recorridoBase[^1],
                    recorridoBase
                        .Skip(1)
                        .Take(recorridoBase.Count - 2)
                        .ToList(),
                    ct);

            ResultadoRuta rutaConEnvios = rutaBase;

            if (compromisos.Envios.Count > 0)
            {
                var recorridoComprometido =
                    _constructor.ConstruirConEnvios(
                        recorridoBase,
                        compromisos.Envios,
                        ct);

                rutaConEnvios =
                    await servicioRutas.CalcularRutaAsync(
                        recorridoComprometido[0],
                        recorridoComprometido[^1],
                        recorridoComprometido
                            .Skip(1)
                            .Take(recorridoComprometido.Count - 2)
                            .ToList(),
                        ct);
            }

            if (!double.IsFinite(rutaBase.DistanciaMetros)
                || rutaBase.DistanciaMetros < 0
                || !double.IsFinite(rutaConEnvios.DistanciaMetros)
                || rutaConEnvios.DistanciaMetros < 0)
            {
                throw new InvalidOperationException(
                    "El servicio de rutas devolvió una distancia inválida.");
            }

            return new EvaluacionDesvio
            {
                RutaBase = rutaBase,
                RutaConEnvios = rutaConEnvios,
                DesvioMaximoKm = desvioMaximoKm
            };
        }
    }
}
