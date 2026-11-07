using System.Globalization;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    // Un paquete dentro del detalle de un viaje.
    public class PaqueteAsignadoViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        public int AsignacionId { get; set; }
        public int EnvioId { get; set; }
        public string Remitente { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public double PesoKg { get; set; }
        public decimal Monto { get; set; }
        public string DireccionRetiro { get; set; } = string.Empty;
        public string DireccionEntrega { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }

        // Estado del ENVIO, no de la asignacion: asi cada paquete muestra en que paso
        // del transporte esta (retirado, en camino, entregado...).
        public EstadoEnvio Estado { get; set; }

        public DateTime? FechaAceptacion { get; set; }
        public DateTime? FechaRetiro { get; set; }
        public DateTime? FechaEntrega { get; set; }

        public string EstadoTexto => Estado switch
        {
            EstadoEnvio.BuscandoConductor => "Buscando conductor",
            EstadoEnvio.ConductorAsignado => "Conductor asignado",
            EstadoEnvio.Retirado => "Retirado",
            EstadoEnvio.EnCamino => "En camino",
            EstadoEnvio.Entregado => "Entregado",
            EstadoEnvio.Cancelado => "Cancelado",
            _ => Estado.ToString()
        };

        public string EstadoClase => Estado switch
        {
            EstadoEnvio.ConductorAsignado => "vd-chip--asignado",
            EstadoEnvio.Retirado => "vd-chip--retirado",
            EstadoEnvio.EnCamino => "vd-chip--encamino",
            EstadoEnvio.Entregado => "vd-chip--confirmado",
            EstadoEnvio.Cancelado => "vd-chip--cancelado",
            _ => "vd-chip--pendiente"
        };

        public string Titulo => string.IsNullOrWhiteSpace(Descripcion) ? $"Envío #{EnvioId}" : Descripcion;

        public string MontoTexto => Monto.ToString("C0", Argentina);
    }
}
