using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades
{
    public class Asignacion
    {
        public int Id { get; set; }
        public int EnvioId { get; set; }
        public Envio Envio { get; set; }
        public int ViajeId { get; set; }
        public Viaje Viaje { get; set; }
        public EstadoViaje Estado { get; set; }
        public DateTime? FechaAceptacion { get; set; }
        public DateTime? FechaRetiro { get; set; }
        public DateTime? FechaEntrega { get; set; }


    }
}
