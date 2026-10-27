using System.ComponentModel.DataAnnotations;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models {
    public class NuevoEnvioViewModel : IValidatableObject {
        [Required(ErrorMessage = "El origen es obligatorio")]
        public string Origen { get; set; } = string.Empty;

        [Required(ErrorMessage = "El destino es obligatorio")]
        public string Destino { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Range(0.01, 10000, ErrorMessage = "El peso debe ser mayor que cero")]
        public double PesoKg { get; set; } = 0;

        [Required(ErrorMessage = "El tamaño es obligatorio")]
        public TamanioEnvio? Tamanio { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Descripcion { get; set; } = string.Empty;

        [Range(0.01, 999999999, ErrorMessage = "El monto debe ser mayor que cero")]
        public decimal Monto { get; set; } = 0m;

        public IEnumerable<ValidationResult> Validate(ValidationContext contexto) {
            if (string.Equals(Origen?.Trim(), Destino?.Trim(), StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult("El origen y el destino deben ser diferentes", new[] { nameof(Destino) });

            if (Fecha.Date < DateTime.Today)
                yield return new ValidationResult("La fecha no puede ser anterior a hoy", new[] { nameof(Fecha) });
        }
    }
}
