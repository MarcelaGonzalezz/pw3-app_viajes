using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas {
    public record ResultadoRuta(double DistanciaMetros, double DuracionSegundos) {
        public double DistanciaKm => DistanciaMetros / 1000.0;
        public double DuracionMinutos => DuracionSegundos / 60.0;
    }
    //devuelve cualquier calculo de ruta( distancia y tiempos totales. la API Here lo entrega en metros y segundos
}
