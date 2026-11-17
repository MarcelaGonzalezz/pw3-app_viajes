using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Ganancias
{
    public sealed class SemanaDeGananciasDTO
    {
        public required int Numero { get; init; }
        public required DateTime Desde { get; init; }
        public required DateTime Hasta { get; init; }

        public required int Viajes { get; init; }
        public required int Paquetes { get; init; }
        public required decimal GananciaPotencial { get; init; }
    }
}
