using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas {
    public class MockServicioRutas : IServicioRutas {

        private readonly RutasOptions _opciones;

        public Task<ResultadoRuta> CalcularRutaAsync(Coordenada origen, Coordenada destino, IReadOnlyList<Coordenada>? paradas = null, CancellationToken ct = default) {
            throw new NotImplementedException();
        }
    }
}
