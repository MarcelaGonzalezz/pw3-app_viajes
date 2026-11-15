using System;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class EnvioDetalleViewModel
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
        public bool TieneConductorAsignado { get; set; }
        public int? ViajeId { get; set; }
        public string? ConductorNombre { get; set; }
        public string? ConductorEmail { get; set; }
        public DateTime? FechaSalidaViaje { get; set; }
        public DateTime? FechaAceptacion { get; set; }
        public DateTime? FechaRetiro { get; set; }
        public DateTime? FechaEntrega { get; set; }

        public bool EstaVencido =>
            Estado == EstadoEnvio.BuscandoConductor && FechaHasta.Date < HoyEnArgentina;

        private static DateTime HoyEnArgentina =>
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires")).Date;

        public string EstadoTexto => EstaVencido ? "Vencido" : Estado switch
        {
            EstadoEnvio.BuscandoConductor => "Buscando conductor",
            EstadoEnvio.ConductorAsignado => "Conductor asignado",
            EstadoEnvio.Retirado => "Retirado",
            EstadoEnvio.EnCamino => "En camino",
            EstadoEnvio.Entregado => "Entregado",
            EstadoEnvio.Cancelado => "Cancelado",
            _ => Estado.ToString()
        };

        public string EstadoBadgeClass => Estado switch
        {
            EstadoEnvio.BuscandoConductor => "vd-estado--activo",
            EstadoEnvio.ConductorAsignado => "vd-estado--activo",
            EstadoEnvio.Retirado => "vd-estado--activo",
            EstadoEnvio.EnCamino => "vd-estado--activo",
            _ => "vd-estado--inactivo"
        };

        public string TamanioTexto => Tamanio switch
        {
            TamanioEnvio.Pequenio => "Pequeño",
            TamanioEnvio.Mediano => "Mediano",
            TamanioEnvio.Grande => "Grande",
            _ => Tamanio.ToString()
        };

        public bool PuedeCancelar => Estado == EstadoEnvio.BuscandoConductor;
    }
}
