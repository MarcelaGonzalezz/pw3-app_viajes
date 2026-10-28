using System.ComponentModel.DataAnnotations;

namespace TpMiercoles.Web.Models
{
        public class DireccionViewModel
        {
            [Required(ErrorMessage = "Seleccioná una dirección.")]
            public string Texto { get; set; } = null!;

            [Range(-90, 90)]
            public decimal Latitud { get; set; }

            [Range(-180, 180)]
            public decimal Longitud { get; set; }
        }
}
