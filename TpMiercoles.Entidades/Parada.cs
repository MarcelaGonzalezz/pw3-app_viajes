using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Entidades
{
    public class Parada
    {
        public int Id { get; set; }
        public int RutaId { get; set; }
        public Ruta Ruta { get; set; } = null!;
        public int DireccionId { get; set; }
        public int Orden { get; set; }
        public Direccion Direccion { get; set; } = null!;

    }
}
