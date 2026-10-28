using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs.Ruta;

namespace TpMiercoles.Servicios.DTOs
{
    public class ViajeDetalleDTO
    {
        public int Id { get; set; }
        public int RutaId { get; set; }
        public EstadoActividadViaje Estado { get; set; }
        public string DireccionOrigen { get; set; } = string.Empty;
        public string DireccionDestino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public double PesoMax { get; set; }
        public int CapacidadMaxPaquetes { get; set; }
        public int CantidadPaquetes { get; set; }
        public int LugaresDisponibles { get; set; }
        public List<DayOfWeek> DiasSemana { get; set; } = [];
        public List<ParadaDTO> Paradas { get; set; } = [];
        public List<PaqueteAsignadoDTO> Paquetes { get; set; } = [];
    }
}
