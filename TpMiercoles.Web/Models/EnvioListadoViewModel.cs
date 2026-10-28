using System;
using TpMiercoles.Entidades;

namespace TpMiercoles.Web.Models
{
    public class EnvioListadoViewModel
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

        public string EstadoTexto => Estado switch
        {
            EstadoEnvio.BuscandoConductor => "Buscando conductor",
            EstadoEnvio.ConductorAsignado => "Conductor asignado",
            EstadoEnvio.Retirado => "Retirado",
            EstadoEnvio.EnCamino => "En camino",
            EstadoEnvio.Entregado => "Entregado",
            EstadoEnvio.Cancelado => "Cancelado",
            _ => Estado.ToString()
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
