using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades
{
    public class Parada
    {
        public int Id { get; set; }

        // Una parada pertenece a una ruta (plantilla) o a un viaje (snapshot), nunca a ambos.
        public int? RutaId { get; set; }
        public Ruta? Ruta { get; set; }

        public int? ViajeId { get; set; }
        public Viaje? Viaje { get; set; }

        public int DireccionId { get; set; }
        public int Orden { get; set; }
        public Direccion Direccion { get; set; } = null!;

    }
}
