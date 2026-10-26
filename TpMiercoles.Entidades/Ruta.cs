using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades
{
    public class Ruta
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;
        public int DireccionOrigenId { get; set; }
        public Direccion DireccionOrigen { get; set; } = null!;
        public int DireccionDestinoId { get; set; }
        public Direccion DireccionDestino { get; set; } = null!;
        public double PesoMax { get; set; }
        public int CapacidadMaxPaquetes { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public List<RutaDia> RutaDias { get; set; } = [];
        public List<Viaje> Viajes { get; set; } = [];
        public List<Parada> Paradas { get; set; } = [];
        public TimeOnly HoraSalida { get; set; }
        public TimeOnly HoraLlegada { get; set; }
    }
}
