using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.DTOs
{
    public class ViajeEdicionDTO
    {
        public int Id { get; set; }

        // Editables: horarios y capacidad.
        public DateTime FechaSalida { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public double PesoMax { get; set; }
        public int CapacidadMaxPaquetes { get; set; }

        // Contexto de solo lectura para mostrar en el formulario.
        public EstadoActividadViaje Estado { get; set; }
        public string DireccionOrigen { get; set; } = string.Empty;
        public string DireccionDestino { get; set; } = string.Empty;
        public int CantidadPaquetes { get; set; }
    }
}
