namespace TpMiercoles.Servicios.DTOs.Ganancias
{
    // Un envío que la estimación propone aceptar en una salida.
    public sealed class PaquetePropuestoDTO
    {
        public required int EnvioId { get; init; }
        public required string Descripcion { get; init; }
        public required double PesoKg { get; init; }
        public required decimal Monto { get; init; }
        public required string Retiro { get; init; }
        public required string Entrega { get; init; }
    }
}
