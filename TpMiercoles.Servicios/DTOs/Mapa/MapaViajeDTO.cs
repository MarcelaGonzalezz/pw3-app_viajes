using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;

namespace TpMiercoles.Servicios.DTOs.Mapa {
    // Lo que necesita el mapa del detalle del viaje: los puntos a marcar y las dos líneas
    // (recorrido habitual y recorrido pasando a retirar/entregar los paquetes).
    public class MapaViajeDTO {
        public List<PuntoMapaDTO> Puntos { get; set; } = [];

        // GeoJSON del recorrido habitual (origen -> paradas -> destino).
        public string? GeometriaBase { get; set; }

        // GeoJSON del recorrido incluyendo retiros y entregas. Null si no hay paquetes.
        public string? GeometriaConEnvios { get; set; }

        public double? DistanciaBaseKm { get; set; }
        public double? DistanciaConEnviosKm { get; set; }

        public double? DesvioKm =>
            DistanciaBaseKm is double baseKm && DistanciaConEnviosKm is double conEnviosKm
                ? Math.Round(conEnviosKm - baseKm, 1)
                : null;
    }
    public class PuntoMapaDTO {
        public TipoPuntoMapa Tipo { get; set; }
        public string Texto { get; set; } = string.Empty;
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }

        // Solo para retiros y entregas.
        public int? EnvioId { get; set; }

        // Solo para paradas.
        public int? Orden { get; set; }
    }

    // Se manda como texto ("Retiro", "Entrega"...) para que el JS no dependa de números.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TipoPuntoMapa {
        Origen,
        Parada,
        Retiro,
        Entrega,
        Destino
    }
}
