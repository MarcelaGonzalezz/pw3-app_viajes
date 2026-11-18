using System.ComponentModel.DataAnnotations;

namespace TpMiercoles.Web.Models
{
    public sealed class AceptarPropuestaViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "La ruta seleccionada no es válida.")]
        public int RutaId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El viaje seleccionado no es válido.")]
        public int? ViajeId { get; set; }

        [Required(ErrorMessage = "Debe indicar la fecha de salida.")]
        public DateTime? FechaSalidaProgramada { get; set; }

        [Required(ErrorMessage = "Debe seleccionar los envíos de la propuesta.")]
        [MinLength(1, ErrorMessage = "La propuesta debe incluir al menos un envío.")]
        public List<int> EnvioIds { get; set; } = [];
    }
}
