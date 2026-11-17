using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Servicios.Excepciones;

namespace TpMiercoles.Servicios.Rutas {
    // Plan B: usa OpenRouteService y, si se agotó la cuota, calcula con el mock en lugar de fallar.
    //  - 403: se terminó la cuota del día → se deja de consultar ORS durante una hora.
    //  - 429: demasiados pedidos por minuto → se deja de consultar ORS durante un minuto.
    // Mientras dura la pausa, las distancias son estimadas (línea recta × factor de calle) y sin trazado.
    // Cualquier otro error de ORS se sigue informando como antes.
    public class ServicioRutasConRespaldo : IServicioRutas {
        private static readonly TimeSpan PausaPorCuotaDiaria = TimeSpan.FromHours(1);
        private static readonly TimeSpan PausaPorLimitePorMinuto = TimeSpan.FromMinutes(1);

        //Implementa IServicioRutas, la misma interfaz que ORS y el mock
        private readonly IServicioRutas _principal;//ORS
        private readonly IServicioRutas _respaldo;//mock
        private readonly EstadoCuotaRutas _estado;
        private readonly TimeProvider _reloj;//TimeProvider es el reloj. Se usa en lugar de DateTime.
        private readonly ILogger<ServicioRutasConRespaldo> _logger;

        public ServicioRutasConRespaldo(
            IServicioRutas principal,
            IServicioRutas respaldo,
            EstadoCuotaRutas estado,
            TimeProvider reloj,
            ILogger<ServicioRutasConRespaldo> logger) {
            _principal = principal;
            _respaldo = respaldo;
            _estado = estado;
            _reloj = reloj;
            _logger = logger;
        }

        public async Task<ResultadoRuta> CalcularRutaAsync(
            Coordenada origen,
            Coordenada destino,
            IReadOnlyList<Coordenada>? paradas = null,
            CancellationToken ct = default) {
            DateTimeOffset ahora = _reloj.GetUtcNow();

            if (_estado.EstaPausado(ahora)) {
                return await _respaldo.CalcularRutaAsync(origen, destino, paradas, ct);
            }

            try {
                return await _principal.CalcularRutaAsync(origen, destino, paradas, ct);
                //el when hace que solo se atrapen esos dos códigos. Cualquier otro error pasa de largo y se informa como antes.
            } catch (RutaNoEncontradaException ex) when (ex.CodigoHttp is 403 or 429) {
                TimeSpan pausa = ex.CodigoHttp == 429 ? PausaPorLimitePorMinuto : PausaPorCuotaDiaria;
                _estado.PausarHasta(ahora + pausa);

                _logger.LogWarning( //LogWarning deja un aviso en la ventana de Salida de Visual Studio.
                    "OpenRouteService respondió {Codigo}: se usan distancias estimadas hasta {Hasta}.",
                    ex.CodigoHttp, ahora + pausa);

                return await _respaldo.CalcularRutaAsync(origen, destino, paradas, ct);
            }
        }
    }
}
