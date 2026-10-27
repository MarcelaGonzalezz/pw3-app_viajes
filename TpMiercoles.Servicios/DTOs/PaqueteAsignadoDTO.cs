using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.DTOs
{
    public class PaqueteAsignadoDTO
    {
        public int AsignacionId { get; set; }
        public int EnvioId { get; set; }
        public string Remitente { get; set; } = string.Empty;
        public string DireccionRetiro { get; set; } = string.Empty;
        public string DireccionEntrega { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public EstadoViaje Estado { get; set; }
        public DateTime? FechaAceptacion { get; set; }
        public DateTime? FechaRetiro { get; set; }
        public DateTime? FechaEntrega { get; set; }
    }
}
