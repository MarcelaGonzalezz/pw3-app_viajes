using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.DTOs
{
    // Un transporte es un paquete que el conductor acepto llevar: la Asignacion
    // vista desde su lado. El estado que se muestra es el del Envio, que es el
    // mismo que ve el remitente en "Mis envios".
    public class TransporteListadoDTO
    {
        public int AsignacionId { get; set; }
        public int ViajeId { get; set; }
        public int EnvioId { get; set; }

        public string Descripcion { get; set; } = string.Empty;
        public double PesoKg { get; set; }
        public TamanioEnvio Tamanio { get; set; }

        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;

        public decimal Ganancia { get; set; }
        public EstadoEnvio Estado { get; set; }

        public string RemitenteNombre { get; set; } = string.Empty;
        public DateTime FechaSalidaViaje { get; set; }

        // Null cuando ya esta entregado: no hay accion siguiente.
        public EstadoEnvio? ProximoEstado { get; set; }
    }
}
