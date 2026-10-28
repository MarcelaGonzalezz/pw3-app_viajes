using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class ViajeListadoViewModel
    {
        public int Id { get; set; }
        public EstadoActividadViaje Estado { get; set; }
        public string DireccionOrigen { get; set; } = string.Empty;
        public string DireccionDestino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public int CantidadPaquetes { get; set; }
        public int CapacidadMaxPaquetes { get; set; }

        public bool EstaActivo => Estado == EstadoActividadViaje.ACTIVO;
        public string EstadoTexto => EstaActivo ? "Activo" : "Inactivo";
    }
}
