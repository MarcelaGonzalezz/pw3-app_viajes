using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios.DTOs.Optimizacion
{
    public sealed class ResultadoAceptacionPropuestaDTO
    {
        public IReadOnlyList<int> EnviosAceptadosIds { get; init; } = [];

        public int? ViajeId { get; init; }

        public int? EnvioFallidoId { get; init; }

        public string? Error { get; init; }

        public bool EsExitoso =>
            Error is null
            && EnvioFallidoId is null
            && ViajeId.HasValue
            && EnviosAceptadosIds.Count > 0;

        public bool EsParcial =>
            !EsExitoso && EnviosAceptadosIds.Count > 0;
    }
}
