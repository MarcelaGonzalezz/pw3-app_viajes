using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades
{
    public class EnvioEstadoHistorial
    {
        public int Id { get; set; }
        public int EnvioId { get; set; }
        public Envio Envio { get; set; } = null!;
        public EstadoViaje EstadoAnterior { get; set; }
        public EstadoViaje EstadoActual { get; set; }
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public DateTime Fecha { get; set; } = DateTime.Now;

    }
}
