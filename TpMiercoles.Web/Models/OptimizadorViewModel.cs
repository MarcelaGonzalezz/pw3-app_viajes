using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using TpMiercoles.Servicios.DTOs.Oportunidades;
using TpMiercoles.Servicios.DTOs.Optimizacion;

namespace TpMiercoles.Web.Models
{
    public sealed class OptimizadorViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar una salida.")]
        [Display(Name = "Viaje a optimizar")]
        public string? SalidaSeleccionada { get; set; }

        [Required(ErrorMessage = "Debe indicar el desvío máximo.")]
        [Range(
            0,
            double.MaxValue,
            ErrorMessage = "El desvío máximo debe ser mayor o igual a cero.")]
        [Display(Name = "Desvío máximo (km)")]
        public double? DesvioMaximoKm { get; set; }

        [BindNever]
        [ValidateNever]
        public List<SalidaDeRutaDTO> Salidas { get; set; } = [];

        [BindNever]
        [ValidateNever]
        public ResultadoOptimizacionDTO? Resultado { get; set; }
    }
}
