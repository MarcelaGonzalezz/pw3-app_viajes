using TpMiercoles.Servicios.DTOs.Oportunidades;

namespace TpMiercoles.Web.Models
{
    public class OportunidadesViewModel
    {
        // formulario
        public List<ViajeListadoViewModel> Viajes { get; set; } = [];
        public int? ViajeId { get; set; }
        public CriterioOrdenOportunidad Orden { get; set; } = CriterioOrdenOportunidad.Ganancia;
        public double? DesvioMaximoKm { get; set; }

        // resultado (null hasta que se elige un viaje)
        public OportunidadesDelViajeViewModel? Resultado { get; set; }
        public string? Error { get; set; }
    }

    public class OportunidadesDelViajeViewModel
    {
        public int ViajeId { get; set; }
        public string ViajeOrigen { get; set; } = string.Empty;
        public string ViajeDestino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public int CapacidadRestante { get; set; }
        public double PesoRestanteKg { get; set; }
        public double DistanciaHabitualKm { get; set; }
        public double TiempoHabitualMin { get; set; }
        public List<string> AvisosDelViaje { get; set; } = [];
        public List<OportunidadViewModel> Oportunidades { get; set; } = [];
    }
}