namespace TpMiercoles.Web.Models
{
    public class OportunidadDetalleViewModel : OportunidadViewModel
    {
        public int RutaId { get; set; }
        public int? ViajeId { get; set; }
        public string RutaOrigen { get; set; } = string.Empty;
        public string RutaDestino { get; set; } = string.Empty;
        public DateTime FechaSalida { get; set; }
        public double DistanciaHabitualKm { get; set; }
        public double DistanciaConEnvioKm { get; set; }
        public double TiempoHabitualMin { get; set; }
        public double TiempoConEnvioMin { get; set; }
        public int CapacidadRestante { get; set; }
        public double PesoRestanteKg { get; set; }
        public List<string> Motivos { get; set; } = [];

        public bool EsAceptable => Motivos.Count == 0;
    }
}
