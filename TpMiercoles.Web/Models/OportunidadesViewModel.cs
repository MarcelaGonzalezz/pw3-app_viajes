using TpMiercoles.Servicios.DTOs.Oportunidades;

namespace TpMiercoles.Web.Models
{
    public class OportunidadesViewModel
    {
        // formulario
        public List<SalidaDeRutaViewModel> Rutas { get; set; } = [];
        public int? RutaId { get; set; }
        public DateTime? Fecha { get; set; }
        public CriterioOrdenOportunidad Orden { get; set; } = CriterioOrdenOportunidad.Ganancia;
        public double? DesvioMaximoKm { get; set; }

        // resultado (null hasta que se elige una ruta)
        public OportunidadesDeRutaViewModel? Resultado { get; set; }
        public string? Error { get; set; }
    }

    public class SalidaDeRutaViewModel
    {
        public int RutaId { get; set; }
        public int? ViajeId { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public DateTime FechaLlegada { get; set; }
    }

    public class OportunidadesDeRutaViewModel
    {
        public int RutaId { get; set; }
        public int? ViajeId { get; set; }
        public string RutaOrigen { get; set; } = string.Empty;
        public string RutaDestino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public int CapacidadRestante { get; set; }
        public double PesoRestanteKg { get; set; }
        public double DistanciaHabitualKm { get; set; }
        public double TiempoHabitualMin { get; set; }
        public List<string> AvisosDeLaSalida { get; set; } = [];
        public List<OportunidadViewModel> Oportunidades { get; set; } = [];
    }
}
