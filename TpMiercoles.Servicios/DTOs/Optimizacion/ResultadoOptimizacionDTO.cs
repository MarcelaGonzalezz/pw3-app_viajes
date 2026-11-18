using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Optimizacion
{
    public enum EstadoResultadoOptimizacion
    {
        Correcto,
        SolicitudInvalida,
        SalidaNoEncontrada,
        SalidaNoHabilitada
    }

    public sealed class ResultadoOptimizacionDTO
    {
        public required EstadoResultadoOptimizacion Estado { get; init; }

        public IReadOnlyList<string> Mensajes { get; init; } = [];

        public IReadOnlyList<PropuestaOptimizacionDTO> Propuestas
        {
            get;
            init;
        } = [];

        public int? RutaId { get; init; }

        public int? ViajeId { get; init; }

        public DateTime? FechaSalidaProgramada { get; init; }

        public DateTime? MomentoEvaluacion { get; init; }

        public decimal IngresoComprometido { get; init; }

        // true si los km de las propuestas son estimados (no se confirmaron con rutas reales).
        public bool DistanciasEstimadas { get; init; }
    }
}
