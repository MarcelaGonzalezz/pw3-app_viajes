using System.ComponentModel.DataAnnotations;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class NuevoPaqueteViewModel
    {
        [Range(0.01, 10000, ErrorMessage = "El peso debe ser mayor que cero")]
        public double PesoKg { get; set; } = 0;

        [Required(ErrorMessage = "El tamaño es obligatorio")]
        public TamanioEnvio? Tamanio { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Descripcion { get; set; } = string.Empty;
    }
}
