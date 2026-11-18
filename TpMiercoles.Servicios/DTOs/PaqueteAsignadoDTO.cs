using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.DTOs
{
    // Un paquete dentro del detalle de un viaje.
    public class PaqueteAsignadoDTO
    {
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

        // Estado del ENVIO (retirado, en camino, entregado...): el mismo que ve el
        // remitente en "Mis envios". Antes este campo traia el estado de la
        // Asignacion (pendiente/confirmado), por eso todos los paquetes decian
        // "Confirmado" aunque ya estuvieran entregados.
        public EstadoEnvio Estado { get; set; }

        public DateTime? FechaAceptacion { get; set; }
        public DateTime? FechaRetiro { get; set; }
        public DateTime? FechaEntrega { get; set; }
    }
}
