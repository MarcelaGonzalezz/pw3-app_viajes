using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;

namespace TpMiercoles.Web.Models
{
    public class RutaViewModel
    {
        public int Id { get; set; }
        public DireccionViewModel DireccionOrigen { get; set; } = null!;
        public DireccionViewModel DireccionDestino { get; set; } = null!;
        public double PesoMax { get; set; }
        public int CapacidadMaxPaquetes { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public List<DayOfWeek> RutaDias { get; set; } = [];
        public List<ParadaViewModel> Paradas { get; set; } = [];
        public TimeOnly HoraSalida { get; set; }
        public TimeOnly HoraLlegada { get; set; }
        public EstadoRuta Estado { get; set; } 
    }
}
