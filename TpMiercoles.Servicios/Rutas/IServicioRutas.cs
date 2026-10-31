using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.Rutas {
    public interface IServicioRutas {
        Task<ResultadoRuta> CalcularRutaAsync(
            Coordenada origen,
            Coordenada destino,
            IReadOnlyList<Coordenada>? paradas = null,//lista solo lectura, es opcional
            CancellationToken ct = default); //permite cancelar las consultas cuando el usuario cierra la pagina
    }
}
