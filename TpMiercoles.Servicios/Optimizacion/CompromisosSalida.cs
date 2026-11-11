using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Entidades;

namespace TpMiercoles.Servicios.Optimizacion
{
    public sealed class CompromisosSalida
    {
        public required IReadOnlyList<Envio> Envios { get; init; }

        public required double PesoComprometidoKg { get; init; }

        public required double PesoDisponibleKg { get; init; }

        public required int CantidadDisponiblePaquetes { get; init; }

        public required decimal IngresoComprometido { get; init; }

        public int CantidadComprometidaPaquetes => Envios.Count;
    }
}
