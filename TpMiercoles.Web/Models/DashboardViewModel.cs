using System.Collections.Generic;

namespace TpMiercoles.Web.Models
{
    public class DashboardViewModel
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public int ProximosViajes { get; set; }
        public int EnviosPendientes { get; set; }
        public int Oportunidades { get; set; }
        // null si no se pudo calcular
        public decimal? PotencialMes { get; set; }
        // Con que criterio se calculo, ej. "Con mayor ganancia, sin límite de desvío"
        public string PotencialDetalle { get; set; } = string.Empty;
        public List<EnvioListadoViewModel> EnviosPendientesLista { get; set; } = [];
        public List<SalidaDeRutaViewModel> ProximosViajesLista { get; set; } = [];
    }
}