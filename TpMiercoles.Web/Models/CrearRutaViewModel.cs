using System.ComponentModel.DataAnnotations;

namespace TpMiercoles.Web.Models {
    public class CrearRutaViewModel : IValidatableObject {
        public DireccionViewModel DireccionOrigen { get; set; } = new();
        public DireccionViewModel DireccionDestino { get; set; } = new();

        [Range(0.01, double.MaxValue, ErrorMessage = "El peso debe ser mayor que cero.")]
        public double PesoMax { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La capacidad debe ser mayor que cero.")]
        public int CapacidadMaxPaquetes { get; set; }

        [Required(ErrorMessage = "Elegí desde qué fecha rige la ruta.")]
        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        [Required(ErrorMessage = "Elegí la hora de salida.")]
        public TimeOnly HoraSalida { get; set; }

        [Required(ErrorMessage = "Elegí la hora estimada de llegada.")]
        public TimeOnly HoraLlegada { get; set; }

        [MinLength(1, ErrorMessage = "Elegí al menos un día de la semana.")]
        public List<DayOfWeek> RutaDias { get; set; } = [];
        public List<ParadaViewModel> Paradas { get; set; } = [];

        // Reglas que comparan dos campos entre sí (no se pueden expresar con una sola anotación).
        public IEnumerable<ValidationResult> Validate(ValidationContext contexto) {
            if (HoraLlegada <= HoraSalida) {
                yield return new ValidationResult(
                    "La hora de llegada tiene que ser posterior a la de salida.",
                    new[] { nameof(HoraLlegada) });
            }

            if (FechaFin.HasValue && FechaFin.Value.Date < FechaInicio.Date) {
                yield return new ValidationResult(
                    "La fecha \"hasta\" no puede ser anterior a la fecha \"desde\".",
                    new[] { nameof(FechaFin) });
            }
        }
    }
}