using System.ComponentModel.DataAnnotations;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class ViajeEdicionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La fecha y hora de salida es obligatoria")]
        [Display(Name = "Salida")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime FechaSalida { get; set; }

        [Display(Name = "Llegada")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? FechaLlegada { get; set; }

        [Display(Name = "Peso máximo (kg)")]
        [Range(0.01, 10000, ErrorMessage = "El peso máximo tiene que ser mayor a cero")]
        public double PesoMax { get; set; }

        [Display(Name = "Capacidad (paquetes)")]
        [Range(1, 500, ErrorMessage = "La capacidad tiene que ser mayor a cero")]
        public int CapacidadMaxPaquetes { get; set; }

        // Contexto de solo lectura, no se postea.
        public EstadoActividadViaje Estado { get; set; }
        public string DireccionOrigen { get; set; } = string.Empty;
        public string DireccionDestino { get; set; } = string.Empty;
        public int CantidadPaquetes { get; set; }

        public bool EstaActivo => Estado == EstadoActividadViaje.ACTIVO;
        public string EstadoTexto => EstaActivo ? "Activo" : "Inactivo";
    }
}
