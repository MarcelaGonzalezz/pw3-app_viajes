using System;
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
        public int TotalRetirados { get; set; }
        public int TotalEnCamino { get; set; }
        public int TotalEntregados { get; set; }
        public int TotalCancelados { get; set; }
        public bool SoloVencidos { get; set; }
        public int TotalVencidos { get; set; }
        public string? Texto { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public bool HayFiltros =>
            Filtro is not null
            || SoloVencidos
            || !string.IsNullOrWhiteSpace(Texto)
            || Desde is not null
            || Hasta is not null;
    }
}
