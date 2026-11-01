using System.Globalization;
using TpMiercoles.Servicios.DTOs.Historial;

namespace TpMiercoles.Web.Models
{
    public class HistorialViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        // Filtros activos. null = "Todos" / sin limite.
        public TipoOperacionHistorial? Filtro { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }

        // Mensaje de validacion del formulario de fechas.
        public string? Error { get; set; }

        // Resumen del periodo
        public int ViajesRealizados { get; set; }
        public int EnviosTransportados { get; set; }
        public decimal GananciaEfectiva { get; set; }

        // Contadores de los chips
        public int TotalOperaciones { get; set; }
        public int TotalEnviosEntregados { get; set; }
        public int TotalEnviosCancelados { get; set; }

        public List<HistorialItemViewModel> Items { get; set; } = [];

        public string GananciaTexto => GananciaEfectiva.ToString("C0", Argentina);

        public decimal PromedioPorViaje => ViajesRealizados == 0 ? 0 : GananciaEfectiva / ViajesRealizados;
        public string PromedioTexto => PromedioPorViaje.ToString("C0", Argentina);

        public bool HayFiltroDeFechas => Desde is not null || Hasta is not null;
    }
}
