namespace TpMiercoles.Servicios.DTOs.Ganancias
{
    public sealed class SalidaEstimadaDTO
    {
        public required int RutaId { get; init; }
        public required DateTime FechaSalida { get; init; }
        public required string Origen { get; init; }
        public required string Destino { get; init; }

        // Etiqueta de la propuesta que se tomo: "Mayor ganancia", "Menor desvío"
        // o "Equilibrada". Null si esa salida no admitio ningun envío.
        public string? PropuestaElegida { get; init; }

        public required IReadOnlyList<int> EnviosIds { get; init; }

        public required decimal GananciaPotencial { get; init; }
        public required decimal GananciaComprometida { get; init; }
        public required double PesoKg { get; init; }
        public required double DesvioKm { get; init; }
        public required double DistanciaKm { get; init; }
        public required double DuracionMinutos { get; init; }
        public required int CapacidadMaximaPaquetes { get; init; }
        public required double PesoMaximoKg { get; init; }

        public int CantidadPaquetes => EnviosIds.Count;

        public bool TuvoEnvios => EnviosIds.Count > 0;
    }
}