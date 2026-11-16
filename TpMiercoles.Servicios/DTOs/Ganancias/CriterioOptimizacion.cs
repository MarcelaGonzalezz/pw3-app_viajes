using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Ganancias
{ // cual de las tres propuestas del optimizador se toma de cada salida, al acumular el potencial del mes
    public enum CriterioOptimizacion
    {
        MayorGanancia = 0,
        MenorDesvio = 1,
        Equilibrada = 2
    }
}
