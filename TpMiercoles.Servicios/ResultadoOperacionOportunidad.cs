using System;
using System.Collections.Generic;
using System.Text;

namespace TpMiercoles.Servicios
{
    // 404 si el viaje o el envio no existen (o el viaje no es del usuario);
    // 409 si las restricciones dejaron de cumplirse entre que viste la oportunidad y la aceptaste
    public class ResultadoOperacionOportunidad
    {
        public bool NoEncontrado { get; private init; }
        public string? Conflicto { get; private init; }
        public int? AsignacionId { get; private init; }
        public int? ViajeId { get; private init; }

        public bool EsExitoso => !NoEncontrado && Conflicto is null;

        public static ResultadoOperacionOportunidad NoExiste() => new() { NoEncontrado = true };

        public static ResultadoOperacionOportunidad EnConflicto(string motivo) => new() { Conflicto = motivo };

        public static ResultadoOperacionOportunidad Exitoso(int asignacionId, int viajeId)
            => new() { AsignacionId = asignacionId, ViajeId = viajeId };
    }
}
