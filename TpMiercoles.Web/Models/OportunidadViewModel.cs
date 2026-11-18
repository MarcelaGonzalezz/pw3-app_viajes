using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class OportunidadViewModel
    {
        public int EnvioId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public TamanioEnvio Tamanio { get; set; }
        public double PesoKg { get; set; }
        public decimal Pago { get; set; }
        public string Retiro { get; set; } = string.Empty;
        public string Entrega { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public string RemitenteNombre { get; set; } = string.Empty;
        public double DesvioKm { get; set; }
        public double TiempoAdicionalMin { get; set; }
        public int Compatibilidad { get; set; }
        public decimal? GananciaPorKmAdicional { get; set; }
    }
}
