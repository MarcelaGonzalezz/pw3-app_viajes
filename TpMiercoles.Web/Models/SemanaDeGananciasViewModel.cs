using System.Globalization;

namespace TpMiercoles.Web.Models
{
    public class SemanaDeGananciasViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        public int Numero { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int Viajes { get; set; }
        public int Paquetes { get; set; }
        public decimal GananciaPotencial { get; set; }

        public string RangoTexto => Desde.Date == Hasta.Date
            ? Desde.ToString("ddd dd/MM", Argentina)
            : $"{Desde:dd/MM} al {Hasta:dd/MM}";

        public string GananciaTexto => GananciaPotencial.ToString("C0", Argentina);
    }
}