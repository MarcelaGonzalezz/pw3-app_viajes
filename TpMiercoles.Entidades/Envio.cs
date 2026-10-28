using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades
{
    public class Envio
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public int DireccionRetiroId { get; set; }
        public int DireccionEntregaId { get; set; }
        public Direccion DireccionRetiro { get; set; } = null!;
        public Direccion DireccionEntrega { get; set; } = null!;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public List<Asignacion> Asignaciones { get; set; } = [];

        public Paquete Paquete { get; set; } = new();
        public decimal Monto { get; set; }
        public EstadoEnvio Estado { get; set; } = EstadoEnvio.BuscandoConductor;


    }
}
