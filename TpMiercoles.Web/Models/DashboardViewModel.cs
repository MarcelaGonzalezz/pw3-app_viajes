using System.Collections.Generic;

namespace TpMiercoles.Web.Models {
    public class DashboardViewModel {
        public string NombreUsuario { get; set; } = string.Empty;
        public int ProximosViajes { get; set; }
        public int EnviosPendientes { get; set; }
        public int Oportunidades { get; set; }
        public decimal PotencialMes { get; set; }
        public List<EnvioListadoViewModel> EnviosPendientesLista { get; set; } = [];
    }
}
