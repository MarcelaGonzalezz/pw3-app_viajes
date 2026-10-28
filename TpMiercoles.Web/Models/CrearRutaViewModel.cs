using System.ComponentModel.DataAnnotations;

namespace TpMiercoles.Web.Models
{
    public class CrearRutaViewModel
    {
        public DireccionViewModel DireccionOrigen { get; set; } = new();
        public DireccionViewModel DireccionDestino { get; set; } = new();

        [Range(0.01, double.MaxValue, ErrorMessage = "El peso debe ser mayor que cero.")]
        public double PesoMax { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser mayor que cero.")]
        public int CapacidadMaxPaquetes { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        [Required]
        public TimeOnly HoraSalida { get; set; }
        [Required]
        public TimeOnly HoraLlegada { get; set; }

        public List<DayOfWeek> RutaDias { get; set; } = [];
        public List<ParadaViewModel> Paradas { get; set; } = [];
    }
}
