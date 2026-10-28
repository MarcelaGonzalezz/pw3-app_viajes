using System.Collections.Generic;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class MisEnviosViewModel
    {
        public EstadoEnvio? Filtro { get; set; }
        public List<EnvioListadoViewModel> Envios { get; set; } = [];

        public int TotalTodos { get; set; }
        public int TotalBuscando { get; set; }
        public int TotalAsignados { get; set; }
        public int TotalEnCamino { get; set; }
        public int TotalEntregados { get; set; }
        public int TotalCancelados { get; set; }
    }
}
