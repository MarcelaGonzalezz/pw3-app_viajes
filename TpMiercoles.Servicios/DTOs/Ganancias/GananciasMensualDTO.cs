namespace TpMiercoles.Servicios.DTOs.Ganancias
{
    public sealed class GananciasMensualDTO
    {
        public required int Anio { get; init; }
        public required int Mes { get; init; }
        public required CriterioOptimizacion Criterio { get; init; }

        public required DateTime EfectivaDesde { get; init; }
        public required DateTime EfectivaHasta { get; init; }
        public required DateTime PotencialDesde { get; init; }
        public required DateTime PotencialHasta { get; init; }

        public required decimal GananciaEfectiva { get; init; } //entregado y cobrado
        public required decimal GananciaComprometida { get; init; } //aceptado pero no entregado
        public required decimal GananciaPotencial { get; init; } //hipotetico con el optimizador

        public required int ViajesAnalizados { get; init; }
        public required int PaquetesPosibles { get; init; }
        public required int EnviosEntregados { get; init; }
        public required double DesvioTotalKm { get; init; }
        public required double PesoTotalKg { get; init; }

        public decimal PromedioPorViaje => ViajesAnalizados == 0 ? 0m : GananciaPotencial / ViajesAnalizados;

        public required IReadOnlyList<SemanaDeGananciasDTO> Semanas { get; init; }
        public required IReadOnlyList<SalidaEstimadaDTO> Salidas { get; init; }
        public SalidaEstimadaDTO? MejorRecorrido { get; init; }
        public required int SalidasDescartadas { get; init; }
        public required int SalidasSinEnviosCompatibles { get; init; }
    }
}