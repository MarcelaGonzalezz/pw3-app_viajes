using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades
{
    public class Viaje
    {
        public int Id { get; set; }
        public int RutaId { get; set; }
        public Ruta Ruta { get; set; } = null!;
        public int DireccionOrigenId { get; set; }
        public Direccion DireccionOrigen { get; set; } = null!;
        public int DireccionDestinoId { get; set; }
        public Direccion DireccionDestino { get; set; } = null!;
        public double PesoMax { get; set; }
        public int CapacidadMaxPaquetes { get; set; }
        public DateTime FechaSalida { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public List<Asignacion> Asignaciones { get; set; } = [];
       
    }
}
