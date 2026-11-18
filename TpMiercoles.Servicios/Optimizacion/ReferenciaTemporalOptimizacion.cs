using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Optimizacion
{
    public static class ReferenciaTemporalOptimizacion
    {
        public static DateTime DeterminarMomentoEvaluacion(DateTime fechaSalidaProgramada, DateTime ahora)
        {
            return ahora >= fechaSalidaProgramada
                ? ahora
                : fechaSalidaProgramada;
        }
    }
}
