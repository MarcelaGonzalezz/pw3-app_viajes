using System.Globalization;
using TpMiercoles.Servicios.DTOs.Ganancias;

namespace TpMiercoles.Web.Models
{
    public class GananciasViewModel
    {
        private static readonly CultureInfo Argentina = new("es-AR");

        public int Anio { get; set; }
        public int Mes { get; set; }
        public CriterioOptimizacion Criterio { get; set; }
        public double DesvioMaximoKm { get; set; }
        public string? Error { get; set; }

        public DateTime EfectivaDesde { get; set; }
        public DateTime EfectivaHasta { get; set; }
        public DateTime PotencialDesde { get; set; }
        public DateTime PotencialHasta { get; set; }

        public decimal GananciaEfectiva { get; set; }
        public decimal GananciaComprometida { get; set; }
        public decimal GananciaPotencial { get; set; }

        public int ViajesAnalizados { get; set; }
        public int PaquetesPosibles { get; set; }
        public int EnviosEntregados { get; set; }
        public double DesvioTotalKm { get; set; }
        public double PesoTotalKg { get; set; }
        public decimal PromedioPorViaje { get; set; }

        public List<SemanaDeGananciasViewModel> Semanas { get; set; } = [];
        public List<SalidaEstimadaViewModel> Salidas { get; set; } = [];
        public SalidaEstimadaViewModel? MejorRecorrido { get; set; }

        public int SalidasDescartadas { get; set; }
        public int SalidasSinEnviosCompatibles { get; set; }

        public string MesTexto => new DateTime(Anio, Mes, 1).ToString("MMMM yyyy", Argentina);

        public bool MesTodaviaNoEmpezo => EfectivaDesde == EfectivaHasta;
        public bool MesTerminado => PotencialDesde > PotencialHasta;

        public string PeriodoEfectivoTexto => $"del {EfectivaDesde:dd/MM} al {EfectivaHasta:dd/MM}";
        public string PeriodoPotencialTexto => $"del {PotencialDesde:dd/MM} al {PotencialHasta:dd/MM}";

        public string GananciaEfectivaTexto => GananciaEfectiva.ToString("C0", Argentina);
        public string GananciaComprometidaTexto => GananciaComprometida.ToString("C0", Argentina);
        public string GananciaPotencialTexto => GananciaPotencial.ToString("C0", Argentina);
        public string PromedioTexto => PromedioPorViaje.ToString("C0", Argentina);
    }
}