using System.Globalization;

namespace TpMiercoles.Web.Models
{
    public class PaquetePropuestoViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        public int EnvioId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public double PesoKg { get; set; }
        public decimal Monto { get; set; }
        public string Retiro { get; set; } = string.Empty;
        public string Entrega { get; set; } = string.Empty;

        public string PesoTexto => $"{PesoKg.ToString("0.#", Argentina)} kg";
        public string MontoTexto => Monto.ToString("C0", Argentina);
    }
}
