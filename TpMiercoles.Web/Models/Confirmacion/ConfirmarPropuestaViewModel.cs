using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TpMiercoles.Servicios.DTOs.Optimizacion;

namespace TpMiercoles.Web.Models {
    // Pantalla 11: confirmación de la propuesta elegida en el optimizador, antes de reservar los envíos.
    public sealed class ConfirmarPropuestaViewModel {
        [Range(1, int.MaxValue, ErrorMessage = "La ruta seleccionada no es válida.")]
        public int RutaId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El viaje seleccionado no es válido.")]
        public int? ViajeId { get; set; }

        [Required(ErrorMessage = "Debe indicar la fecha de salida.")]
        public DateTime? FechaSalidaProgramada { get; set; }

        [Required(ErrorMessage = "Debe seleccionar los envíos de la propuesta.")]
        [MinLength(1, ErrorMessage = "La propuesta debe incluir al menos un envío.")]
        public List<int> EnvioIds { get; set; } = [];

        // Datos de la propuesta calculada por el optimizador (desvío y totales del viaje con los envíos).
        public double DesvioTotalKm { get; set; }
        public int CantidadTotalPaquetes { get; set; }
        public bool DistanciasEstimadas { get; set; }

        // Para volver al optimizador con la misma búsqueda ("Volver" y "Recalcular").
        public string? SalidaSeleccionada { get; set; }
        public double? DesvioMaximoKm { get; set; }

        // Lo arma el servidor con los datos actuales de cada envío.
        [BindNever]
        [ValidateNever]
        public ResumenPropuestaDTO? Resumen { get; set; }
    }
}