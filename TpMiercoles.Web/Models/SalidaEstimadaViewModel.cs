using System.Globalization;

namespace TpMiercoles.Web.Models
{
    public class SalidaEstimadaViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        public int RutaId { get; set; }
        public DateTime FechaSalida { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public string? PropuestaElegida { get; set; }
        public List<int> EnviosIds { get; set; } = [];

        public decimal GananciaPotencial { get; set; }
        public decimal GananciaComprometida { get; set; }
        public double PesoKg { get; set; }
        public double DesvioKm { get; set; }
        public double DistanciaKm { get; set; }
        public double DuracionMinutos { get; set; }
        public int CapacidadMaximaPaquetes { get; set; }
        public double PesoMaximoKg { get; set; }

        public int CantidadPaquetes { get; set; }
        public bool TuvoEnvios { get; set; }

        public string FechaTexto => FechaSalida.ToString("ddd dd/MM · HH:mm", Argentina);
        public string GananciaTexto => GananciaPotencial.ToString("C0", Argentina);
        public string ComprometidaTexto => GananciaComprometida.ToString("C0", Argentina);
        public string OcupacionTexto => $"{CantidadPaquetes} de {CapacidadMaximaPaquetes} paquetes";
        public string PesoTexto => $"{PesoKg:0.#} de {PesoMaximoKg:0.#} kg";
        public string DesvioTexto => $"{DesvioKm:0.#} km";
    }
}