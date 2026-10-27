using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class PaqueteAsignadoViewModel
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

        public string EstadoTexto => Estado switch
        {
            EstadoViaje.PENDIENTE => "Pendiente",
            EstadoViaje.CONFIRMADO => "Confirmado",
            EstadoViaje.CANCELADO => "Cancelado",
            _ => Estado.ToString()
        };

        public string EstadoClase => Estado switch
        {
            EstadoViaje.PENDIENTE => "vd-chip--pendiente",
            EstadoViaje.CONFIRMADO => "vd-chip--confirmado",
            EstadoViaje.CANCELADO => "vd-chip--cancelado",
            _ => "vd-chip--pendiente"
        };
    }
}
