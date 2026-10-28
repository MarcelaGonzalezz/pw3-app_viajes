using System;
using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.DTOs
{
    public class EnvioListadoDTO
    {
        public int Id { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public double PesoKg { get; set; }
        public TamanioEnvio Tamanio { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public EstadoEnvio Estado { get; set; }
    }
}
