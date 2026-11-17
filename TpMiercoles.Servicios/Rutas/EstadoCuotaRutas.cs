using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas {
    // Recuerda hasta cuándo no conviene pedirle rutas a OpenRouteService porque se agotó la cuota.
    // Se registra como singleton: lo comparten todos los pedidos de la aplicación.
    public class EstadoCuotaRutas {
        //Es una clase aparte porque tiene que ser una sola para toda la app.
        //Si cada pedido tuviera su propia memoria, todos volverían a probar ORS sin saber que se agotó la cuota.
        private readonly object _bloqueo = new();
        private DateTimeOffset _pausadoHasta = DateTimeOffset.MinValue;

        public bool EstaPausado(DateTimeOffset ahora) {
            lock (_bloqueo) { //El lock evita problemas si dos usuarios hacen pedidos exactamente al mismo tiempo.
                return ahora < _pausadoHasta;
            }
        }

        public void PausarHasta(DateTimeOffset hasta) {
            lock (_bloqueo) {
                if (hasta > _pausadoHasta) {
                    _pausadoHasta = hasta;
                }
            }
        }
    }
}
