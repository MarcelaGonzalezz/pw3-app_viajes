using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Optimizacion
{
    public sealed class SolicitudOptimizacionDTO
    {
        public required int RutaId { get; init; }

        public int? ViajeId { get; init; }

        public required double DesvioMaximoKm { get; init; }
    }
}
