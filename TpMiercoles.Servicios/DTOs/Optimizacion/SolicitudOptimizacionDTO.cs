using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Optimizacion
{
    public sealed class SolicitudOptimizacionDTO
    {
        public required int RutaId { get; init; }
        public int? ViajeId { get; init; }
        public DateTime? FechaSalida { get; init; }
        public required double DesvioMaximoKm { get; init; }
        public IReadOnlyList<int>? EnviosExcluidos { get; init; }

        // true: se calcula todo con distancias estimadas, sin consultar rutas reales.
        // Lo usa Potencial de ganancias, que analiza muchas salidas por carga.
        public bool SoloEstimado { get; init; }
    }
}
