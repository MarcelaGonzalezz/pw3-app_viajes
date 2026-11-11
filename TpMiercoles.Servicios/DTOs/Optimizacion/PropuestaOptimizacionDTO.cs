using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Optimizacion
{
    public sealed class PropuestaOptimizacionDTO
    {
        public List<string> Etiquetas { get; init; } = [];

        public required IReadOnlyList<int> EnviosNuevosIds { get; init; }

        public required decimal IngresoAdicional { get; init; }

        public required decimal IngresoTotal { get; init; }

        public required double PesoAdicionalKg { get; init; }

        public required double PesoTotalKg { get; init; }

        public required int CantidadTotalPaquetes { get; init; }

        public required double DesvioTotalKm { get; init; }

        public required double DistanciaTotalKm { get; init; }

        public required double DuracionTotalMinutos { get; init; }
    }
}
