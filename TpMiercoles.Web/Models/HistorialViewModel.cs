using System.Globalization;
using TpMiercoles.Entidades;
using TpMiercoles.Servicios.DTOs.Historial;

namespace TpMiercoles.Web.Models
{
    public class HistorialViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        // Filtros activos. null = "Todos" / sin limite.
        public TipoOperacionHistorial? Filtro { get; set; }
        public EstadoActividadViaje? Estado { get; set; }
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

        public List<HistorialItemViewModel> Items { get; set; } = [];

        public string GananciaTexto => GananciaEfectiva.ToString("C0", Argentina);

        public decimal PromedioPorViaje => ViajesRealizados == 0 ? 0 : GananciaEfectiva / ViajesRealizados;
        public string PromedioTexto => PromedioPorViaje.ToString("C0", Argentina);

        public bool HayFiltroDeFechas => Desde is not null || Hasta is not null;
        public bool HayFiltros => Filtro is not null || Estado is not null || HayFiltroDeFechas;

        // ---- Atajos de periodo (hora de Argentina) ----

        private static DateTime HoyEnArgentina =>
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires")).Date;

        private static DateTime PrimerDia(DateTime d) => new(d.Year, d.Month, 1);
        private static DateTime UltimoDia(DateTime d) => new(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));

        public DateTime EsteMesDesde => PrimerDia(HoyEnArgentina);
        public DateTime EsteMesHasta => UltimoDia(HoyEnArgentina);
        public DateTime MesAnteriorDesde => PrimerDia(HoyEnArgentina.AddMonths(-1));
        public DateTime MesAnteriorHasta => UltimoDia(HoyEnArgentina.AddMonths(-1));

        public bool EsTodo => !HayFiltroDeFechas;
        public bool EsEsteMes => Desde?.Date == EsteMesDesde && Hasta?.Date == EsteMesHasta;
        public bool EsMesAnterior => Desde?.Date == MesAnteriorDesde && Hasta?.Date == MesAnteriorHasta;

        // Rotulo para la tarjeta de ganancia: deja claro de que periodo es el numero.
        public string PeriodoTexto
        {
            get
            {
                if (EsTodo) return "Total histórico";
                if (EsEsteMes) return "Este mes";
                if (EsMesAnterior) return "Mes anterior";
                if (Desde is not null && Hasta is not null) return $"Del {Desde:dd/MM/yyyy} al {Hasta:dd/MM/yyyy}";
                if (Desde is not null) return $"Desde el {Desde:dd/MM/yyyy}";
                return $"Hasta el {Hasta:dd/MM/yyyy}";
            }
        }
    }
}