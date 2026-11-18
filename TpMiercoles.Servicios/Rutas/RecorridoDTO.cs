using System;
using System.Collections.Generic;
using System.Text;
using TpMiercoles.Servicios.DTOs.Direccion;
using TpMiercoles.Servicios.DTOs.Ruta;
using TpMiercoles.Servicios.Rutas;

namespace TpMiercoles.Servicios.Rutas {
    public class RecorridoDTO {
        public DireccionDTO Origen { get; set; } = null!;
        public DireccionDTO Destino { get; set; } = null!;
        public List<ParadaDTO> Paradas { get; set; } = [];
    }

    public interface IRecorridoServicio {
        Task<ResultadoRuta> CalcularAsync(RecorridoDTO recorrido, CancellationToken ct = default);
    }

    public class RecorridoServicio : IRecorridoServicio {
        private readonly IServicioRutas _rutas;

        public RecorridoServicio(IServicioRutas rutas) => _rutas = rutas;

        public Task<ResultadoRuta> CalcularAsync(RecorridoDTO recorrido, CancellationToken ct = default) {
            var paradas = recorrido.Paradas
                .OrderBy(p => p.Orden)
                .Select(p => ACoordenada(p.Direccion))
                .ToList();

            return _rutas.CalcularRutaAsync(
                ACoordenada(recorrido.Origen),
                ACoordenada(recorrido.Destino),
                paradas,
                ct);
        }

        private static Coordenada ACoordenada(DireccionDTO d) => new(d.Latitud, d.Longitud);
    }
}
