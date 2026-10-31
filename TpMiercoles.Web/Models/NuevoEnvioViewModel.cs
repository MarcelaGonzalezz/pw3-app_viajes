using System.ComponentModel.DataAnnotations;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models {
    public class NuevoEnvioViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "El origen es obligatorio")]
        public DireccionViewModel Origen { get; set; } = null!;

        [Required(ErrorMessage = "El destino es obligatorio")]
        public DireccionViewModel Destino { get; set; } = null!;

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime? FechaHasta { get; set; } = DateTime.Today;

        public NuevoPaqueteViewModel Paquete { get; set; } = new();

        [Range(0.01, 999999999, ErrorMessage = "El monto debe ser mayor que cero")]
        public decimal Monto { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext contexto)
        {
            var origen = Origen?.Texto?.Trim();
            var destino = Destino?.Texto?.Trim();

            if (!string.IsNullOrEmpty(origen) &&
                string.Equals(origen, destino, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "El origen y el destino deben ser diferentes",
                    new[] { $"{nameof(Destino)}.{nameof(Destino.Texto)}" });
            }

            if (FechaHasta.HasValue && FechaHasta.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "La fecha no puede ser anterior a hoy",
                    new[] { nameof(FechaHasta) });
            }
        }
    }
}
